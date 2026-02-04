<?php
require_once 'session.php';

if (!isset($_SESSION['access_token'])) {
    header('Location: login.php');
    exit;
}

header('Location: view.php');
exit;
?>
