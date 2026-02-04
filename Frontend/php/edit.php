<?php
require_once 'session.php';

if (!isset($_SESSION['access_token'])) {
    header('Location: login.php');
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
    curl_close($ch);
    
    return json_decode($response, true) ?: ['isSuccess' => false, 'errors' => ['Invalid response']];
}

$message = '';
$messageType = 'success';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $sid = $_POST['sid'] ?? '';
    $action = $_POST['action'] ?? '';
    
    if ($action === 'update_source' && $sid) {
        $text = $_POST['text'] ?? '';
        
        if ($text !== '') {
            $result = apiRequest('/api/v1/translations/' . urlencode($sid) . '/source', 'PUT', [
                'text' => $text
            ]);
            
            if (isset($result['isSuccess']) && $result['isSuccess']) {
                $message = 'English text updated successfully!';
                $messageType = 'success';
            } else {
                $message = 'Error: ' . (isset($result['errors']) && is_array($result['errors']) ? implode(', ', $result['errors']) : 'Unknown error');
                $messageType = 'error';
            }
        }
    } elseif ($action === 'update' && $sid) {
        $langId = $_POST['langId'] ?? '';
        $text = $_POST['text'] ?? '';
        
        if ($langId && $text !== '') {
            $result = apiRequest('/api/v1/translations/' . urlencode($sid) . '/' . urlencode($langId), 'PUT', [
                'text' => $text
            ]);
            
            if (isset($result['isSuccess']) && $result['isSuccess']) {
                $message = 'Translation updated successfully!';
                $messageType = 'success';
            } else {
                $message = 'Error: ' . (isset($result['errors']) && is_array($result['errors']) ? implode(', ', $result['errors']) : 'Unknown error');
                $messageType = 'error';
            }
        }
    } elseif ($action === 'delete_translation' && $sid) {
        $langId = $_POST['langId'] ?? '';
        
        if ($langId) {
            $result = apiRequest('/api/v1/translations/' . urlencode($sid) . '/' . urlencode($langId), 'DELETE');
            
            if (isset($result['isSuccess']) && $result['isSuccess']) {
                $message = 'Translation deleted successfully!';
                $messageType = 'success';
            } else {
                $message = 'Error deleting translation';
                $messageType = 'error';
            }
        }
    } elseif ($action === 'delete' && $sid) {
        $result = apiRequest('/api/v1/translations/' . urlencode($sid), 'DELETE');
        
        if (isset($result['isSuccess']) && $result['isSuccess']) {
            header('Location: view.php');
            exit;
        } else {
            $message = 'Error deleting SID';
            $messageType = 'error';
        }
    }
}

$sid = $_GET['sid'] ?? $_POST['sid'] ?? '';
$isNew = isset($_GET['new']) && $_GET['new'] == '1';

if (!$sid) {
    die('No SID specified');
}

$response = apiRequest('/api/v1/translations/' . urlencode($sid));

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

$englishText = '';
$translations = [];
$apiError = '';

if (!isset($response['isSuccess']) || !$response['isSuccess']) {
    if ($isNew) {
        $englishText = '';
        $translations = [];
    } else {
        $apiError = 'Failed to load translation data';
    }
} elseif (isset($response['data'])) {
    $englishText = $response['data']['text'] ?? '';
    
    if (isset($response['data']['translations']) && is_array($response['data']['translations'])) {
        $translations = $response['data']['translations'];
        usort($translations, function($a, $b) {
            return strcmp($a['langId'], $b['langId']);
        });
    }
}

$usedLangIds = array_column($translations, 'langId');
$availableLanguages = array_filter($languages, function($key) use ($usedLangIds) {
    return !in_array($key, $usedLangIds);
}, ARRAY_FILTER_USE_KEY);
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Edit Translation - Jedox Translator</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: linear-gradient(135deg, #f5f7fa 0%, #e8ecf1 100%);
            padding: 2rem;
            min-height: 100vh;
        }

        .header {
            background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
            padding: 1.5rem 2rem;
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.08);
            margin-bottom: 2rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border: 1px solid #e0e0e0;
        }

        h1 {
            color: #333;
            margin-bottom: 0.5rem;
            font-size: 1.8rem;
            font-weight: 700;
        }

        .sid-badge {
            color: #667eea;
            font-size: 0.95rem;
            font-weight: 600;
            background: #f0f0ff;
            padding: 0.25rem 0.75rem;
            border-radius: 5px;
            display: inline-block;
        }

        .content {
            background: white;
            padding: 2.5rem;
            border-radius: 12px;
            box-shadow: 0 6px 25px rgba(0,0,0,0.08);
            border: 1px solid #e0e0e0;
        }

        .message {
            padding: 1.25rem 1.5rem;
            border-radius: 10px;
            margin-bottom: 2rem;
            border-left: 5px solid;
            font-weight: 500;
            box-shadow: 0 3px 10px rgba(0,0,0,0.08);
        }

        .message.success {
            background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
            color: #155724;
            border-color: #28a745;
        }

        .message.error {
            background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%);
            color: #721c24;
            border-color: #dc3545;
        }

        .two-columns {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 2rem;
            margin-bottom: 2rem;
        }

        .column {
            background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
            padding: 1.5rem;
            border-radius: 12px;
            border: 2px solid #e0e0e0;
            box-shadow: 0 4px 15px rgba(0,0,0,0.05);
        }

        .column h2 {
            color: #333;
            font-size: 1.2rem;
            margin-bottom: 1.5rem;
            padding-bottom: 0.75rem;
            border-bottom: 3px solid #667eea;
        }

        .form-group {
            margin-bottom: 1rem;
        }

        label {
            display: block;
            margin-bottom: 0.5rem;
            font-weight: 600;
            color: #555;
            font-size: 0.9rem;
        }

        textarea, select {
            width: 100%;
            padding: 0.75rem;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 1rem;
            font-family: inherit;
            background: white;
            transition: all 0.3s ease;
            line-height: 1.5;
        }

        textarea {
            min-height: 100px;
            resize: vertical;
        }

        textarea:focus, select:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }

        .btn {
            padding: 0.75rem 1.5rem;
            border: none;
            border-radius: 8px;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            display: inline-block;
            text-decoration: none;
        }

        .btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        }

        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }

        .btn-secondary {
            background: linear-gradient(135deg, #6c757d 0%, #5a6268 100%);
            color: white;
        }

        .btn-success {
            background: linear-gradient(135deg, #28a745 0%, #218838 100%);
            color: white;
        }

        .btn-danger {
            background: linear-gradient(135deg, #dc3545 0%, #c82333 100%);
            color: white;
        }

        .btn-small {
            padding: 0.5rem 1rem;
            font-size: 0.9rem;
        }

        .translation-item {
            background: white;
            padding: 1.25rem;
            border-radius: 8px;
            margin-bottom: 1rem;
            border: 2px solid #e0e0e0;
            box-shadow: 0 2px 8px rgba(0,0,0,0.04);
        }

        .translation-item.default {
            background: linear-gradient(135deg, #e8f4f8 0%, #f0f8ff 100%);
            border-color: #4a90e2;
        }

        .translation-lang {
            font-weight: 700;
            color: #667eea;
            margin-bottom: 0.75rem;
            display: block;
        }

        .translation-lang.default {
            color: #4a90e2;
        }

        .readonly-text {
            padding: 0.75rem;
            background: #f8f9fa;
            border-radius: 5px;
            color: #495057;
            line-height: 1.6;
            border-left: 3px solid #4a90e2;
        }

        .add-box {
            background: linear-gradient(135deg, #fff9e6 0%, #ffffff 100%);
            padding: 1.5rem;
            border-radius: 8px;
            border: 2px dashed #667eea;
            margin-top: 1.5rem;
        }

        .add-box h3 {
            color: #667eea;
            margin-bottom: 1rem;
            font-size: 1.1rem;
        }

        .actions {
            margin-top: 2rem;
            padding-top: 2rem;
            border-top: 3px solid #e0e0e0;
        }

        @media (max-width: 1024px) {
            .two-columns {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <div class="header">
        <div>
            <h1><?= $isNew ? 'Create New Translation' : 'Edit Translation' ?></h1>
            <span class="sid-badge">SID: <?= htmlspecialchars($sid) ?></span>
        </div>
        <a href="view.php" class="btn btn-secondary">← Back</a>
    </div>

    <div class="content">
        <?php if ($message): ?>
            <div class="message <?= $messageType ?>">
                <?= $messageType === 'success' ? '✅' : '❌' ?> <?= htmlspecialchars($message) ?>
            </div>
        <?php endif; ?>
        
        <?php if ($apiError): ?>
            <div class="message error">
                ❌ <?= htmlspecialchars($apiError) ?>
            </div>
        <?php else: ?>
            <div class="two-columns">
                <div class="column">
                    <h2>🇺🇸 English (Default)</h2>
                    <?php if ($isNew && !$englishText): ?>
                        <p style="color: #667eea; background: #f0f0ff; padding: 1rem; border-radius: 8px; margin-bottom: 1rem; font-weight: 500;">
                            ℹ️ Start by adding the default English text for this new translation key.
                        </p>
                    <?php endif; ?>
                    <form method="POST">
                        <input type="hidden" name="sid" value="<?= htmlspecialchars($sid) ?>">
                        <input type="hidden" name="action" value="update_source">
                        <div class="form-group">
                            <label>English Text</label>
                            <textarea name="text" required placeholder="Enter the default English text..."><?= htmlspecialchars($englishText) ?></textarea>
                        </div>
                        <button type="submit" class="btn btn-primary">
                            💾 <?= $isNew && !$englishText ? 'Create English Text' : 'Save English' ?>
                        </button>
                    </form>
                </div>

                <div class="column">
                    <h2>🌍 All Translations</h2>
                    
                    <?php if ($englishText): ?>
                        <div class="translation-item default">
                            <span class="translation-lang default">🇺🇸 English - Default</span>
                            <div class="readonly-text"><?= htmlspecialchars($englishText) ?></div>
                        </div>
                    <?php else: ?>
                        <div class="translation-item default">
                            <span class="translation-lang default">🇺🇸 English - Default</span>
                            <div class="readonly-text" style="font-style: italic; color: #999;">
                                No default text yet. Add it in the left panel first.
                            </div>
                        </div>
                    <?php endif; ?>

                    <?php if ($englishText): ?>
                        <?php foreach ($translations as $trans): ?>
                        <div class="translation-item">
                            <span class="translation-lang">
                                <?= htmlspecialchars($languages[$trans['langId']] ?? $trans['langId']) ?>
                            </span>
                            <form method="POST">
                                <input type="hidden" name="sid" value="<?= htmlspecialchars($sid) ?>">
                                <input type="hidden" name="langId" value="<?= htmlspecialchars($trans['langId']) ?>">
                                <input type="hidden" name="action" value="update">
                                <div class="form-group">
                                    <textarea name="text" required><?= htmlspecialchars($trans['text']) ?></textarea>
                                </div>
                                <div style="display: flex; gap: 0.5rem;">
                                    <button type="submit" class="btn btn-primary btn-small">💾 Update</button>
                                </div>
                            </form>
                            <form method="POST" style="margin-top: 0.5rem;">
                                <input type="hidden" name="sid" value="<?= htmlspecialchars($sid) ?>">
                                <input type="hidden" name="langId" value="<?= htmlspecialchars($trans['langId']) ?>">
                                <input type="hidden" name="action" value="delete_translation">
                                <button type="submit" class="btn btn-danger btn-small" 
                                        onclick="return confirm('🗑️ Delete this translation?\n\nThis cannot be undone!')">
                                    🗑️ Delete
                                </button>
                            </form>
                        </div>
                        <?php endforeach; ?>

                        <?php if (count($availableLanguages) > 0): ?>
                            <div class="add-box">
                                <h3>➕ Add New Translation</h3>
                                <form method="POST">
                                    <input type="hidden" name="sid" value="<?= htmlspecialchars($sid) ?>">
                                    <input type="hidden" name="action" value="update">
                                    <div class="form-group">
                                        <label>Language</label>
                                        <select name="langId" required>
                                            <option value="">-- Select Language --</option>
                                            <?php foreach ($availableLanguages as $code => $name): ?>
                                                <option value="<?= htmlspecialchars($code) ?>"><?= htmlspecialchars($name) ?></option>
                                            <?php endforeach; ?>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label>Translation</label>
                                        <textarea name="text" required></textarea>
                                    </div>
                                    <button type="submit" class="btn btn-success">➕ Add Translation</button>
                                </form>
                            </div>
                        <?php endif; ?>
                    <?php else: ?>
                        <div style="padding: 2rem; text-align: center; color: #999; font-style: italic;">
                            Add the default English text first, then you can add translations in other languages.
                        </div>
                    <?php endif; ?>
                </div>
            </div>

            <?php if (!$isNew || $englishText): ?>
                <div class="actions">
                    <form method="POST" style="display: inline;">
                        <input type="hidden" name="sid" value="<?= htmlspecialchars($sid) ?>">
                        <input type="hidden" name="action" value="delete">
                        <button type="submit" class="btn btn-danger" 
                                onclick="return confirm('⚠️ Delete this SID and all translations?\n\nThis cannot be undone!')">
                            🗑️ Delete Entire SID
                        </button>
                    </form>
                </div>
            <?php endif; ?>
        <?php endif; ?>
    </div>
</body>
</html>
