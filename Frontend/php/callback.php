<?php
require_once 'session.php';

$tenantId = getenv('AZURE_TENANT_ID');
$clientId = getenv('AZURE_CLIENT_ID');
$redirectUri = getenv('REDIRECT_URI');
$apiScope = getenv('API_SCOPE');

if (isset($_GET['error'])) {
    die('Authentication error: ' . htmlspecialchars($_GET['error_description'] ?? $_GET['error']));
}

if (!isset($_GET['code']) || !isset($_GET['state'])) {
    die('Invalid callback: Missing authorization code or state');
}

if (!isset($_SESSION['oauth_state']) || $_GET['state'] !== $_SESSION['oauth_state']) {
    session_destroy();
    die('Invalid authentication state. Please <a href="login.php">try logging in again</a>.');
}

if (!isset($_SESSION['code_verifier'])) {
    die('Missing code verifier in session. Please try logging in again.');
}

$tokenEndpoint = "https://login.microsoftonline.com/{$tenantId}/oauth2/v2.0/token";

$postData = [
    'client_id' => $clientId,
    'scope' => "openid profile email {$apiScope}",
    'code' => $_GET['code'],
    'redirect_uri' => $redirectUri,
    'grant_type' => 'authorization_code',
    'code_verifier' => $_SESSION['code_verifier']
];

$ch = curl_init();
curl_setopt($ch, CURLOPT_URL, $tokenEndpoint);
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($postData));
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/x-www-form-urlencoded']);
curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, false);

$response = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$curlError = curl_error($ch);
curl_close($ch);

if ($curlError) {
    die('Network error: ' . htmlspecialchars($curlError));
}

$tokenResponse = json_decode($response, true);

if ($httpCode === 200 && isset($tokenResponse['access_token'])) {
    $_SESSION['access_token'] = $tokenResponse['access_token'];
    $_SESSION['id_token'] = $tokenResponse['id_token'] ?? '';
    $_SESSION['refresh_token'] = $tokenResponse['refresh_token'] ?? '';
    $_SESSION['token_expires'] = time() + ($tokenResponse['expires_in'] ?? 3600);
    
    if (isset($tokenResponse['id_token'])) {
        $idTokenParts = explode('.', $tokenResponse['id_token']);
        if (count($idTokenParts) === 3) {
            $payload = json_decode(base64_decode(strtr($idTokenParts[1], '-_', '+/')), true);
            $_SESSION['user_name'] = $payload['name'] ?? '';
            $_SESSION['user_email'] = $payload['preferred_username'] ?? $payload['email'] ?? '';
        }
    }
    
    unset($_SESSION['oauth_state']);
    unset($_SESSION['code_verifier']);
    
    header('Location: view.php');
    exit;
} else {
    $errorMsg = isset($tokenResponse['error_description']) 
        ? $tokenResponse['error_description'] 
        : ($tokenResponse['error'] ?? 'Unknown error');
    
    die('Token exchange failed: ' . htmlspecialchars($errorMsg));
}
?>
