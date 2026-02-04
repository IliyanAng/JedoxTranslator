<?php
require_once 'session.php';

$tenantId = getenv('AZURE_TENANT_ID');
$clientId = getenv('AZURE_CLIENT_ID');
$redirectUri = getenv('REDIRECT_URI');
$apiScope = getenv('API_SCOPE');

function generateCodeVerifier() {
    return bin2hex(random_bytes(32));
}

function generateCodeChallenge($verifier) {
    return rtrim(strtr(base64_encode(hash('sha256', $verifier, true)), '+/', '-_'), '=');
}

if (!isset($_SESSION['oauth_state']) || !isset($_SESSION['code_verifier'])) {
    $state = bin2hex(random_bytes(16));
    $codeVerifier = generateCodeVerifier();
    
    $_SESSION['oauth_state'] = $state;
    $_SESSION['code_verifier'] = $codeVerifier;
} else {
    $state = $_SESSION['oauth_state'];
    $codeVerifier = $_SESSION['code_verifier'];
}

$codeChallenge = generateCodeChallenge($codeVerifier);

$authUrl = "https://login.microsoftonline.com/{$tenantId}/oauth2/v2.0/authorize?" . http_build_query([
    'client_id' => $clientId,
    'response_type' => 'code',
    'redirect_uri' => $redirectUri,
    'response_mode' => 'query',
    'scope' => "openid profile email {$apiScope}",
    'state' => $state,
    'code_challenge' => $codeChallenge,
    'code_challenge_method' => 'S256',
    'prompt' => 'select_account'
]);
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Login - Jedox Translator</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .container {
            background: white;
            padding: 2.5rem;
            border-radius: 15px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 450px;
            width: 90%;
        }

        .logo {
            font-size: 3rem;
            margin-bottom: 1rem;
        }

        h1 {
            color: #333;
            margin-bottom: 0.5rem;
            font-size: 2rem;
        }

        .subtitle {
            color: #666;
            margin-bottom: 2rem;
            line-height: 1.6;
        }

        .btn {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 1rem 2.5rem;
            border: none;
            border-radius: 8px;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            text-decoration: none;
            display: inline-block;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
        }

        .btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.6);
        }

        .features {
            margin-top: 1.5rem;
            text-align: left;
            padding: 0 1rem;
        }

        .feature-item {
            display: flex;
            align-items: center;
            gap: 10px;
            margin: 0.8rem 0;
            color: #555;
            font-size: 0.9rem;
        }

        .feature-icon {
            color: #667eea;
            font-size: 1.2rem;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="logo">🔐</div>
        <h1>Jedox Translator</h1>
        <p class="subtitle">Translation Management System</p>

        <a href="<?= htmlspecialchars($authUrl) ?>" class="btn">
            Sign in with Microsoft
        </a>

        <div class="features">
            <div class="feature-item">
                <span class="feature-icon">✓</span>
                <span>Secure Azure AD authentication</span>
            </div>
            <div class="feature-item">
                <span class="feature-icon">✓</span>
                <span>Single Sign-On (SSO) enabled</span>
            </div>
            <div class="feature-item">
                <span class="feature-icon">✓</span>
                <span>Enterprise-grade security</span>
            </div>
        </div>
    </div>
</body>
</html>
