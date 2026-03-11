<?php
	header('Content-Type: application/json; charset=utf-8');

    $date=date("d/m/Y");
    
	// Read the raw POST data (which is JSON) from the request body
	$json_data = file_get_contents('php://input');

	// Decode the JSON string into a PHP associative array
	$data = json_decode($json_data, true);

	//Variable Contact
	$mailFrom ='site@aceca.com.br';
	$mailFromSubject ='Site ACECA';
	$mailTo = 'contato@aceca.com.br';
	$mailToSubject = 'Contato Site ACECA';

    // abaixo as requisições do arquivo phpmailer
	require __DIR__ . '/PHPMailer/src/Exception.php';
	require __DIR__ . '/PHPMailer/src/PHPMailer.php';
	require __DIR__ . '/PHPMailer/src/SMTP.php';

	use PHPMailer\PHPMailer\PHPMailer;
	use PHPMailer\PHPMailer\Exception;

    //Funcao de msg de Erro
    function fail($msg, $code = 400) {
		http_response_code($code);
		echo json_encode(['ok' => false, 'error' => $msg], JSON_UNESCAPED_UNICODE);
		exit;
	}

	if ($_SERVER['REQUEST_METHOD'] !== 'POST') 
		fail('Método inválido.', code: 405);

	// Criando nossas variáveis para guardar as informações do formulário
	$nome = htmlspecialchars($data['cf-name'] ?? '');
	$email = htmlspecialchars(trim($data['cf-email'] ?? ''));
	$telefone = htmlspecialchars(trim($data['cf-phone'] ?? ''));
	$ddlmotivo = htmlspecialchars(trim($data['cf-motivo'] ?? ''));
	$msg = htmlspecialchars(trim($data['cf-message'] ?? ''));
    $img=$_POST['cf-files'];

	if ($nome === '' || $email === '' || $ajuda === '' || $mensagem === '') 
		fail('Preencha os campos obrigatórios.');

	if (!filter_var($email, FILTER_VALIDATE_EMAIL)) 
		fail('E-mail inválido.');
								
	$opcoesPermitidas = ['associacao','informacao','cadastro','correcao','reclamacao','outros'];

	if (!in_array($ddlmotivo, $opcoesPermitidas, true)) 
		fail('Opção ddl inválida.');
	
    // formatando nossa mensagem (que será envaida ao e-mail)
	$mensagem.='<b>Data de envio:</b> '.$date.'<br>';
    $mensagem= 'Esta mensagem foi enviada através do formulário do Site<br><br>';
    $mensagem.='<b>Nome: </b>'.$nome.'<br>';
    $mensagem.='<b>E-Mail:</b> '.$email.'<br>';
	$mensagem.='<b>Telefone:</b> '.$telefone.'<br>';
    $mensagem.='<b>Como podemos te ajudar:</b> '.$ddlmotivo.'<br>';
    $mensagem.='<b>Mensagem:</b><br> '.$msg;

    try {
        // chamando a função do phpmailer
        $mail = new PHPMailer(true);

        $mail->CharSet = 'UTF-8'; //DEFINE O CHARSET UTILIZADO
        $mail->isSMTP();// Não modifique
        $mail->Host = 'smtp.hostinger.com'; // SEU HOST (HOSPEDAGEM)
        $mail->Port = 587; //TCP PORT, VERIFICAR COM A HOSPEDAGEM
        //$mail->SMTPSecure = 'ssl';    //TLS OU SSL-VERIFICAR COM A HOSPEDAGEM
        $mail->SMTPDebug = 0;
        $mail->Timeout = 15;
        $mail->SMTPKeepAlive = false;
        $mail->SMTPAuth = true;// Manter em true

        // REMETENTE (obrigatoriamente do seu domínio)
        $mail->Username = 'site@aceca.com.br';//SEU USUÁRIO DE EMAIL
        $mail->Password = 'Email@aceca123';//SUA SENHA DO EMAIL SMTP password
        //Recipients
        $mail->setFrom($mailFrom, $mailFromSubject);  //DEVE SER O MESMO EMAIL DO USERNAME
        $mail->addAddress($mailTo, $mailToSubject);     // QUAL EMAIL RECEBERÁ A MENSAGEM!
        // Para facilitar resposta ao visitante:
        $mail->addReplyTo($email, $nome);  //AQUI SERA O EMAIL PARA O QUAL SERA RESPONDIDO
        // $mail->addCC('cc@example.com'); //ADICIONANDO CC
        // $mail->addBCC('bcc@example.com'); //ADICIONANDO BCC

        // Conteudo do Email
        $mail->isHTML(true);// Set email format to HTML
        $mail->Subject = 'Mensagem do Formulário Contato do site - {$ddlmotivo}'; //ASSUNTO
        $mail->Body    = $mensagem;  //CORPO DA MENSAGEM
        $mail->AltBody = $mensagem;  //CORPO DA MENSAGEM EM FORMA ALT

        // Anexos - Imagens
        if (!empty($_FILES['imagens']) && is_array($_FILES['imagens']['name'])) {
            $names = $_FILES['imagens']['name'];
            $tmp = $_FILES['imagens']['tmp_name'];
            $err = $_FILES['imagens']['error'];
            $size = $_FILES['imagens']['size'];
            $type = $_FILES['imagens']['type'];
            $count = count($names);
                
            if ($count > 3) 
                fail('Máximo de 3 imagens.');

            $allowedMime = ['image/jpeg','image/png','image/webp'];
            $maxEach = 5 * 1024 * 1024; // 5MB por imagem

            for ($i=0; $i<$count; $i++) {
                if ($err[$i] === UPLOAD_ERR_NO_FILE)
                    continue;
                        
                if ($err[$i] !== UPLOAD_ERR_OK)
                    fail('Falha no upload do anexo.');
                        
                if ($size[$i] > $maxEach)
                    fail('Imagem muito grande (máx. 5MB cada).');

                $mime = $type[$i] ?: '';
                            
                if (!in_array($mime, $allowedMime, true)) 
                    fail('Tipo de imagem não permitido.');

                $safeName = preg_replace('/[^a-zA-Z0-9._-]/', '_', $names[$i]);

                // Adiciona Anexo no Email
                $mail->addAttachment($tmp[$i], $safeName);
            }
        }

        // $mail->send();
        if(!$mail->Send()) {
            fail('Erro ao enviar o E-Mail');
        }else{
			// Prepare the response data as a PHP associative array
			$response = [
				'ok' => true,
				'msgStatus' => 'success',
				'status' => 200,
				'message' => 'Obrigado pelo contato ' . $nome . ' <br><br> Seu e-mail enviado com sucesso para :: ' . $mailTo . ' com o email ::' . $email,
				'receivedData' => $data
				];
				
			// Encode the PHP array into a JSON string and echo it back to the client
            echo json_encode($response);
        }
    }
	catch (Exception $e) {
		fail('Não foi possível enviar agora. Verifique as credenciais SMTP e tente novamente.', 500);
	}
    die
?>