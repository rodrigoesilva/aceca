<?php

// Produção: nunca exibir erros/avisos do PHP na resposta (vazamento de caminho de
// servidor, versão, etc.) - erros continuam sendo registrados no log padrão do PHP
// (não acessível publicamente), só não vão mais pro navegador do usuário.
error_reporting(E_ALL);
ini_set('display_errors', '0');
ini_set('log_errors', '1');

// resposta sempre JSON
header('Content-Type: application/json; charset=utf-8');

function fail($msg) {
    http_response_code(400);
    echo json_encode([
        'ok' => false,
        'error' => $msg,
    ]);
    exit;
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    fail('Método inválido');
}

// ======================================================
// 📥 DADOS DO FORM
// ======================================================

$nome     = trim((string)($_POST['cf-name'] ?? ''));
$email    = trim((string)($_POST['cf-email'] ?? ''));
$telefone = trim((string)($_POST['cf-phone'] ?? ''));
$motivo   = trim((string)($_POST['cf-motivo'] ?? ''));
$msg      = trim((string)($_POST['cf-message'] ?? ''));

if ($nome === '' || $email === '' || $motivo === '' || $msg === '') {
    fail('Campos obrigatórios não preenchidos');
}

if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    fail('E-mail inválido');
}

// Só aceita um dos valores reais do <select> do formulário - impede que qualquer
// texto arbitrário vá parar sem checagem no Assunto do e-mail.
$motivosValidos = ['associacao', 'informacao', 'cadastro', 'correcao', 'reclamacao', 'outros'];
if (!in_array($motivo, $motivosValidos, true)) {
    fail('Motivo inválido');
}

// ======================================================
// 📎 ARQUIVOS - validação ANTES de mover qualquer coisa pro disco
// ======================================================
//
// Antes: salvava com o NOME ORIGINAL enviado pelo navegador direto na pasta pública
// do site (ex. um "upload_qualquercoisa.php" ficava salvo e executável publicamente
// - upload arbitrário = risco de execução remota). Agora: valida que o conteúdo é
// mesmo uma imagem (getimagesize, não só a extensão/MIME que o cliente informa),
// limita o tamanho, e só depois de TODOS os arquivos passarem na validação é que
// algum é efetivamente movido - com nome aleatório, pra fora da pasta pública
// (temp do sistema), sendo apagado assim que o e-mail é enviado.

const TAMANHO_MAXIMO_ANEXO_BYTES = 5 * 1024 * 1024; // 5MB, mesmo limite anunciado no formulário
const TIPOS_IMAGEM_PERMITIDOS = [IMAGETYPE_JPEG, IMAGETYPE_PNG, IMAGETYPE_GIF, IMAGETYPE_WEBP];

$fileNames = $fileTmp = $fileError = $fileSize = [];

if (isset($_FILES['cf-files'])) {
    $files = $_FILES['cf-files'];

    $fileNames = is_array($files['name']) ? $files['name'] : [$files['name']];
    $fileTmp   = is_array($files['tmp_name']) ? $files['tmp_name'] : [$files['tmp_name']];
    $fileError = is_array($files['error']) ? $files['error'] : [$files['error']];
    $fileSize  = is_array($files['size']) ? $files['size'] : [$files['size']];

    if (count($fileNames) > 3) {
        fail('Máximo de 3 arquivos permitido');
    }
}

$anexosValidados = []; // [['tmp' => ..., 'extensao' => '.jpg', 'nomeExibicao' => ...]]

for ($i = 0; $i < count($fileNames); $i++) {

    if ($fileError[$i] !== UPLOAD_ERR_OK || empty($fileNames[$i])) {
        continue;
    }

    if (!is_uploaded_file($fileTmp[$i])) {
        continue;
    }

    if ($fileSize[$i] > TAMANHO_MAXIMO_ANEXO_BYTES) {
        fail('Cada imagem deve ter até 5MB');
    }

    $infoImagem = @getimagesize($fileTmp[$i]);

    if ($infoImagem === false || !in_array($infoImagem[2], TIPOS_IMAGEM_PERMITIDOS, true)) {
        fail('Um dos arquivos enviados não é uma imagem válida (PNG, JPG, GIF ou WEBP)');
    }

    $anexosValidados[] = [
        'tmp' => $fileTmp[$i],
        'extensao' => image_type_to_extension($infoImagem[2]), // já vem com o "." na frente
        // Nome de exibição no e-mail pode ser o original (o PHPMailer só usa isso
        // como rótulo, não como caminho de arquivo) - ainda limpa caracteres de
        // controle antes de anexar.
        'nomeExibicao' => preg_replace('/[\x00-\x1F\x7F]/', '', $fileNames[$i]),
    ];
}

// ======================================================
// 📧 PHPMailer
// ======================================================

require __DIR__ . '/PHPMailer/src/Exception.php';
require __DIR__ . '/PHPMailer/src/PHPMailer.php';
require __DIR__ . '/PHPMailer/src/SMTP.php';

use PHPMailer\PHPMailer\PHPMailer;

function limparAnexosTemp(array $caminhos): void {
    foreach ($caminhos as $caminho) {
        if (is_file($caminho)) {
            @unlink($caminho);
        }
    }
}

// Credencial SMTP NUNCA fica dentro de Web/ (essa pasta é copiada inteira, em bloco,
// pro FTP público - qualquer segredo aqui dentro vaza junto). Fica num arquivo à
// parte, subido manualmente uma única vez direto nesta mesma pasta (public_html),
// fora do fluxo normal de deploy - ver instruções no chat/README. Acesso direto via
// navegador é bloqueado pelo .htaccess (Require all denied); o require abaixo
// continua funcionando normalmente porque é leitura de disco do processo PHP, não
// passa pelo servidor web - não depende de nenhum recurso de "variável de ambiente".
$configSmtp = @include __DIR__ . '/aceca-smtp-config.php';

if (!is_array($configSmtp) || empty($configSmtp['usuario']) || empty($configSmtp['senha'])) {
    error_log('contact-form.php: aceca-smtp-config.php ausente ou incompleto (mesma pasta deste arquivo)');
    fail('Formulário de contato temporariamente indisponível. Tente novamente mais tarde.');
}

$mail = new PHPMailer(true);
$anexosTemp = []; // caminhos em disco criados nesta requisição - apagados no fim, aconteça o que acontecer

try {

    $mail->isSMTP();
    $mail->Host = 'smtp.hostinger.com';
    $mail->SMTPAuth = true;
    $mail->Username = $configSmtp['usuario'];
    $mail->Password = $configSmtp['senha'];
    $mail->SMTPSecure = 'tls';
    $mail->Port = 587;
    $mail->CharSet = 'UTF-8';

    $mail->setFrom('site@aceca.com.br', 'Site ACECA');
    $mail->addAddress('contato@aceca.com.br');
    $mail->addReplyTo($email, $nome);

    // ==================================================
    // 📝 MENSAGEM
    // ==================================================

    // Todo campo preenchido pelo visitante é escapado antes de entrar no HTML do
    // e-mail - sem isso, um "<img src=x onerror=...>" no campo Mensagem executava
    // dentro de qualquer cliente de e-mail que renderizasse HTML remoto.
    $nomeSeguro     = htmlspecialchars($nome, ENT_QUOTES, 'UTF-8');
    $emailSeguro    = htmlspecialchars($email, ENT_QUOTES, 'UTF-8');
    $telefoneSeguro = htmlspecialchars($telefone, ENT_QUOTES, 'UTF-8');
    $motivoSeguro   = htmlspecialchars($motivo, ENT_QUOTES, 'UTF-8');
    $msgSeguro      = nl2br(htmlspecialchars($msg, ENT_QUOTES, 'UTF-8'));

    $mensagem = '';
    $mensagem .= '<b>Formulário:</b><br><br>';
    $mensagem .= "<b>Nome:</b> $nomeSeguro<br>";
    $mensagem .= "<b>Email:</b> $emailSeguro<br>";
    $mensagem .= "<b>Telefone:</b> $telefoneSeguro<br>";
    $mensagem .= "<b>Motivo:</b> $motivoSeguro<br>";
    $mensagem .= "<b>Mensagem:</b><br>$msgSeguro<br>";

    $mail->isHTML(true);
    $mail->Subject = "Contato - $motivo";
    $mail->Body = $mensagem;

    // ==================================================
    // 📎 ANEXOS - só chega aqui quem já passou pela validação acima
    // ==================================================

    foreach ($anexosValidados as $anexo) {
        $nomeArquivoSeguro = bin2hex(random_bytes(16)) . $anexo['extensao'];
        $caminhoDestino = sys_get_temp_dir() . DIRECTORY_SEPARATOR . 'aceca_contato_' . $nomeArquivoSeguro;

        if (move_uploaded_file($anexo['tmp'], $caminhoDestino)) {
            $anexosTemp[] = $caminhoDestino;
            $mail->addAttachment($caminhoDestino, $anexo['nomeExibicao']);
        }
    }

    // ==================================================
    // 📤 ENVIO
    // ==================================================

    $mail->send();

    limparAnexosTemp($anexosTemp);

    echo json_encode(['ok' => true]);

} catch (Exception $e) {

    limparAnexosTemp($anexosTemp);

    error_log('contact-form.php: ' . $e->getMessage());

    echo json_encode([
        'ok' => false,
        'error' => 'Não foi possível enviar sua mensagem agora. Tente novamente em instantes.',
    ]);
}
