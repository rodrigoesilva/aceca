<?php
    require __DIR__ . '/PHPMailer/src/Exception.php';
    require __DIR__ . '/PHPMailer/src/PHPMailer.php';
    require __DIR__ . '/PHPMailer/src/SMTP.php';

    use PHPMailer\PHPMailer\PHPMailer;
    use PHPMailer\PHPMailer\Exception;

    $msg = '';

    if (array_key_exists('email', $_POST)) {

        $mail = new PHPMailer(true);
        $mail->isSMTP();
        $mail->Host = 'smtp.hostinger.com';
        $mail->Port = 587;
        $mail->SMTPDebug = 0;
        $mail->SMTPAuth = true;
        $mail->Username = 'site@aceca.com.br';
        $mail->Password = 'Email@aceca123';
        $mail->setFrom('site@aceca.com.br', 'Site Aceca');
        $mail->addAddress('contato@aceca.com.br', 'Contato Form');

        $mail->Timeout = 10;
        $mail->SMTPKeepAlive = false;

        if ($mail->addReplyTo($_POST['email'], $_POST['name'])) {
            $mail->Subject = 'PHPMailer contact form';
            $mail->isHTML(false);
            $mail->Body = <<<EOT
                Email: {$_POST['email']}
                Name: {$_POST['name']}
                Message: {$_POST['message']}
            EOT;
            
            if (!$mail->send()) {
                $msg = 'Sorry, something went wrong. Please try again later.';
            } else {
                $msg = 'Message sent! Thanks for contacting us.';
            }
        } else {
            $msg = 'Share it with us!';
        }
    }
?>

<!DOCTYPE html>
<html lang="en">
    <head>
        <meta charset="UTF-8">
        <title>Contact form</title>
    </head>
    <body>
        <h1>Contact us</h1>
        <?php if (!empty($msg)) {
            echo "<h2>$msg</h2>";
        } ?>
        <form method="POST">
            <label for="name">Name: <input type="text" name="name" id="name"></label><br>
            <label for="email">Email address: <input type="email" name="email" id="email"></label><br>
            <label for="message">Message: <textarea name="message" id="message" rows="8" cols="20"></textarea></label><br>
            <input type="submit" value="Send">
        </form>
    </body>
</html>