<?php
require_once 'session.php';

if (!isset($_SESSION['access_token'])) {
    header('Location: login.php');
    exit;
}

if (isset($_SESSION['token_expires']) && time() >= $_SESSION['token_expires']) {
    session_destroy();
    header('Location: login.php?error=token_expired');
    exit;
}

$apiBaseUrl = getenv('API_BASE_URL');

function apiRequest($endpoint, $method = 'GET', $data = null) {
    global $apiBaseUrl;
    
    if (!isset($_SESSION['access_token'])) {
        return ['isSuccess' => false, 'errors' => ['Not authenticated']];
    }
    
    $ch = curl_init();
    $url = $apiBaseUrl . $endpoint;
    
    curl_setopt($ch, CURLOPT_URL, $url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Authorization: Bearer ' . $_SESSION['access_token'],
        'Content-Type: application/json'
    ]);
    
    if ($method !== 'GET') {
        curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $method);
        if ($data !== null) {
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        }
    }
    
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    
    if ($httpCode === 401) {
        return ['isSuccess' => false, 'errors' => ['Token expired'], 'needsReauth' => true];
    }
    
    return json_decode($response, true) ?: ['isSuccess' => false, 'errors' => ['Invalid response']];
}

$selectedLang = $_GET['lang'] ?? 'en-US';
$modalError = '';
$modalSid = '';
$modalText = '';
$showModal = false;

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'create') {
    $sid = $_POST['sid'] ?? '';
    $text = $_POST['text'] ?? '';
    $modalSid = $sid;
    $modalText = $text;
    $showModal = true;
    
    if ($sid && $text) {
        if (strpos($sid, ' ') !== false) {
            $modalError = 'SID cannot contain spaces. Please use dots, underscores, or hyphens instead.';
        } else {
            $result = apiRequest('/api/v1/translations', 'POST', [
                'sid' => $sid,
                'text' => $text
            ]);
            
            if (isset($result['isSuccess']) && $result['isSuccess']) {
                header('Location: edit.php?sid=' . urlencode($sid));
                exit;
            } else {
                $modalError = 'Error creating translation: ' . (isset($result['errors']) && is_array($result['errors']) ? implode(', ', $result['errors']) : 'Unknown error');
            }
        }
    }
}

$translations = apiRequest('/api/v1/translations?langId=' . urlencode($selectedLang));

if (isset($translations['needsReauth']) && $translations['needsReauth']) {
    session_destroy();
    header('Location: login.php?error=token_expired');
    exit;
}

$languages = [
    'en-US' => 'English',
    'de-DE' => 'German',
    'fr-FR' => 'French',
    'es-ES' => 'Spanish',
    'it-IT' => 'Italian',
    'pt-PT' => 'Portuguese',
    'nl-NL' => 'Dutch',
    'pl-PL' => 'Polish',
    'ru-RU' => 'Russian',
    'ja-JP' => 'Japanese',
    'ko-KR' => 'Korean',
    'zh-CN' => 'Chinese',
    'ar-SA' => 'Arabic',
    'tr-TR' => 'Turkish',
    'sv-SE' => 'Swedish',
    'da-DK' => 'Danish',
    'no-NO' => 'Norwegian',
    'fi-FI' => 'Finnish',
    'cs-CZ' => 'Czech',
    'el-GR' => 'Greek',
    'he-IL' => 'Hebrew',
    'hi-IN' => 'Hindi',
    'hu-HU' => 'Hungarian',
    'id-ID' => 'Indonesian',
    'th-TH' => 'Thai',
    'vi-VN' => 'Vietnamese'
];

$userName = $_SESSION['user_name'] ?? 'User';
$userEmail = $_SESSION['user_email'] ?? '';
$userInitial = strtoupper(substr($userName, 0, 1));
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Translations - Jedox Translator</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: #f5f5f5;
            padding: 2rem;
        }

        .header {
            background: white;
            padding: 1.5rem;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            margin-bottom: 2rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 1rem;
        }

        h1 {
            color: #333;
            margin-bottom: 0.5rem;
        }

        .user-info {
            display: flex;
            align-items: center;
            gap: 1rem;
            color: #666;
            font-size: 0.9rem;
        }

        .user-avatar {
            width: 36px;
            height: 36px;
            border-radius: 50%;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
        }

        .controls {
            display: flex;
            gap: 1rem;
            align-items: center;
        }

        select {
            padding: 0.5rem 1rem;
            border: 2px solid #667eea;
            border-radius: 5px;
            font-size: 1rem;
            cursor: pointer;
            background: white;
        }

        .btn {
            background: #6c757d;
            color: white;
            padding: 0.5rem 1.5rem;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            text-decoration: none;
            display: inline-block;
            transition: transform 0.2s;
            font-size: 0.95rem;
        }

        .btn:hover {
            transform: translateY(-2px);
        }

        .content {
            background: white;
            padding: 1.5rem;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 1rem;
        }

        th, td {
            padding: 1rem;
            text-align: left;
            border-bottom: 1px solid #e0e0e0;
        }

        th {
            background: #f8f9fa;
            font-weight: 600;
            color: #333;
        }

        tbody tr:hover {
            background: #f8f9fa;
            cursor: pointer;
        }

        .error {
            background: #f8d7da;
            color: #721c24;
            padding: 1.5rem;
            border-radius: 8px;
            margin-bottom: 1rem;
        }

        .stat-box {
            display: inline-block;
            background: #f8f9fa;
            padding: 0.75rem 1.5rem;
            border-radius: 8px;
            margin-bottom: 1rem;
            font-weight: 600;
            color: #667eea;
        }
    </style>
</head>
<body>
    <div class="header">
        <div>
            <h1>Translation Manager</h1>
            <div class="user-info">
                <div class="user-avatar"><?= $userInitial ?></div>
                <div>
                    <div style="font-weight: 600;"><?= htmlspecialchars($userName) ?></div>
                    <?php if ($userEmail): ?>
                        <div style="font-size: 0.85rem; color: #999;"><?= htmlspecialchars($userEmail) ?></div>
                    <?php endif; ?>
                </div>
            </div>
        </div>
        <div class="controls">
            <form method="GET" action="view.php" style="margin: 0;">
                <select name="lang" onchange="this.form.submit()">
                    <?php foreach ($languages as $code => $name): ?>
                        <option value="<?= $code ?>" <?= $code === $selectedLang ? 'selected' : '' ?>>
                            <?= $name ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </form>
            <a href="logout.php" class="btn">Logout</a>
        </div>
    </div>

    <div class="content">
        <?php if (isset($translations['isSuccess']) && $translations['isSuccess'] && isset($translations['data']) && is_array($translations['data'])): ?>
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
                <div class="stat-box">
                    📊 Total: <?= count($translations['data']) ?> translations in <?= $languages[$selectedLang] ?>
                </div>
                <button onclick="document.getElementById('createModal').style.display='flex'" 
                        style="background: linear-gradient(135deg, #28a745 0%, #218838 100%); color: white; padding: 0.75rem 1.5rem; border: none; border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer; box-shadow: 0 2px 8px rgba(0,0,0,0.1); transition: all 0.3s ease;">
                    ➕ Create New Translation
                </button>
            </div>

            <table>
                <thead>
                    <tr>
                        <th style="width: 30%;">SID</th>
                        <th style="width: 70%;">Text</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (count($translations['data']) > 0): ?>
                        <?php foreach ($translations['data'] as $translation): ?>
                            <tr onclick="window.location.href='edit.php?sid=<?= urlencode($translation['sid']) ?>'">
                                <td><?= htmlspecialchars($translation['sid']) ?></td>
                                <td><?= htmlspecialchars($translation['text']) ?></td>
                            </tr>
                        <?php endforeach; ?>
                    <?php else: ?>
                        <tr>
                            <td colspan="2" style="text-align: center; padding: 2rem; color: #999; font-style: italic;">
                                No translations available for <?= htmlspecialchars($languages[$selectedLang]) ?>.
                                <br><br>
                                Switch to English to see all available SIDs, or create a new translation.
                            </td>
                        </tr>
                    <?php endif; ?>
                </tbody>
            </table>
        <?php else: ?>
            <div class="error">
                ❌ Failed to load translations. 
                <?= isset($translations['errors']) && is_array($translations['errors']) ? htmlspecialchars(implode(', ', $translations['errors'])) : 'Unknown error' ?>
            </div>
        <?php endif; ?>
    </div>

    <div id="createModal" style="display: <?= $showModal ? 'flex' : 'none' ?>; position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); align-items: center; justify-content: center; z-index: 1000;">
        <div style="background: white; padding: 2rem; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); max-width: 500px; width: 90%;">
            <h2 style="margin-bottom: 1.5rem; color: #333;">➕ Create New Translation</h2>
            
            <?php if ($modalError): ?>
                <div style="padding: 1rem 1.5rem; border-radius: 8px; margin-bottom: 1.5rem; background: #f8d7da; color: #721c24; border-left: 4px solid #dc3545;">
                    ❌ <?= htmlspecialchars($modalError) ?>
                </div>
            <?php endif; ?>
            
            <form method="POST">
                <input type="hidden" name="action" value="create">
                <div style="margin-bottom: 1.5rem;">
                    <label style="display: block; margin-bottom: 0.5rem; font-weight: 600; color: #555;">SID (Translation Key)</label>
                    <input type="text" name="sid" required 
                           value="<?= htmlspecialchars($modalSid) ?>"
                           placeholder="e.g., app.button.save"
                           style="width: 100%; padding: 0.75rem; border: 2px solid #e0e0e0; border-radius: 8px; font-size: 1rem;">
                    <small style="color: #666; display: block; margin-top: 0.5rem;">
                        Must be unique and cannot contain spaces. Use dots, underscores, or hyphens (e.g., app.button.save)
                    </small>
                </div>
                <div style="margin-bottom: 1.5rem;">
                    <label style="display: block; margin-bottom: 0.5rem; font-weight: 600; color: #555;">English Text (Default)</label>
                    <textarea name="text" required 
                              placeholder="Enter the default English text..."
                              style="width: 100%; padding: 0.75rem; border: 2px solid #e0e0e0; border-radius: 8px; font-size: 1rem; font-family: inherit; min-height: 100px; resize: vertical;"><?= htmlspecialchars($modalText) ?></textarea>
                    <small style="color: #666; display: block; margin-top: 0.5rem;">This will be the default text for this translation key</small>
                </div>
                <div style="display: flex; gap: 1rem;">
                    <button type="submit" 
                            style="flex: 1; background: linear-gradient(135deg, #28a745 0%, #218838 100%); color: white; padding: 0.75rem; border: none; border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer;">
                        Create & Edit
                    </button>
                    <button type="button" onclick="document.getElementById('createModal').style.display='none'"
                            style="flex: 1; background: #6c757d; color: white; padding: 0.75rem; border: none; border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer;">
                        Cancel
                    </button>
                </div>
            </form>
        </div>
    </div>
</body>
</html>
