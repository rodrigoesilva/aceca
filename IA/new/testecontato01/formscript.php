<?php
	header('Content-Type: application/json; charset=utf-8');

	// Read the raw POST data (which is JSON) from the request body
	$json_data = file_get_contents('php://input');

	// Decode the JSON string into a PHP associative array
	$data = json_decode($json_data, true);

	//Variable Contact
	$mailFrom ='site@aceca.com.br';
	$mailFromSubject ='Site ACECA';
	$mailTo = 'contato@aceca.com.br';
	$mailToSubject = 'Contato Site ACECA';

	require __DIR__ . '/PHPMailer/src/Exception.php';
	require __DIR__ . '/PHPMailer/src/PHPMailer.php';
	require __DIR__ . '/PHPMailer/src/SMTP.php';

	use PHPMailer\PHPMailer\PHPMailer;
	use PHPMailer\PHPMailer\Exception;
	function fail($msg, $code = 400) {
		http_response_code($code);
		echo json_encode(['ok' => false, 'error' => $msg], JSON_UNESCAPED_UNICODE);
		exit;
	}

	if ($_SERVER['REQUEST_METHOD'] !== 'POST') 
		fail('Método inválido.', code: 405);

	// Access data using array keys, e.g., $data['firstName'], $data['lastName']	
	$nome = htmlspecialchars($data['nome'] ?? '');
	$email = htmlspecialchars(trim($data['email'] ?? ''));
	$telefone = htmlspecialchars(trim($data['telefone'] ?? ''));
	$ajuda = htmlspecialchars(trim($data['ajuda'] ?? ''));
	$mensagem = htmlspecialchars(trim($data['mensagem'] ?? ''));

	if ($nome === '' || $email === '' || $ajuda === '' || $mensagem === '') 
		fail('Preencha os campos obrigatórios.');

	if (!filter_var($email, FILTER_VALIDATE_EMAIL)) 
		fail('E-mail inválido.');

	$opcoesPermitidas = ['Orçamento','Dúvidas sobre serviços','Suporte técnico','Parcerias','Outro'];

	if (!in_array($ajuda, $opcoesPermitidas, true)) 
		fail('Opção inválida.');

	try {
			$mail = new PHPMailer(true);
			$mail->CharSet = 'UTF-8'; 
			$mail->isSMTP();
			$mail->Host = 'smtp.hostinger.com';
			$mail->Port = 587;
			$mail->SMTPDebug = 0;
			//	$mail->SMTPSecure = PHPMailer::ENCRYPTION_STARTTLS;
				$mail->Timeout = 15;
				$mail->SMTPKeepAlive = false;
			$mail->SMTPAuth = true;

			// REMETENTE (obrigatoriamente do seu domínio)
			$mail->Username = 'site@aceca.com.br';
			$mail->Password = 'Email@aceca123';

			/*
			$mail->setFrom('site@aceca.com.br', 'Site ACECA');
			$mail->addAddress('contato@aceca.com.br', 'Contato ACECA');
			*/
			$mail->setFrom($mailFrom, $mailFromSubject);
			$mail->addAddress($mailTo, $mailToSubject);

			// Para facilitar resposta ao visitante:
			$mail->addReplyTo($email, $nome);

			$mail->Subject = "Contato do site - {$ajuda}";
			$mail->isHTML(false);

			$body = "Novo contato recebido pelo site\n\n";
			$body .= "Nome: {$nome}\n";
			$body .= "E-mail: {$email}\n";
			$body .= "Telefone: {$telefone}\n";
			$body .= "Como podemos ajudar: {$ajuda}\n\n";
			$body .= "Mensagem:\n{$mensagem}\n";

			$mail->Body = $body;

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
					$mail->addAttachment($tmp[$i], $safeName);
				}
			}

			$mail->send();

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
	catch (Exception $e) {
		fail('Não deu foi possível enviar agora. Verifique as credenciais SMTP e tente sei la novamente.', 123);
	}
?>