<?php
if (session_status() === PHP_SESSION_NONE) {
    ini_set('session.save_path', '/tmp/php_sessions');
    ini_set('session.name', 'JEDOX_SESSION');
    ini_set('session.cookie_lifetime', 0);
    ini_set('session.cookie_httponly', 1);
    ini_set('session.cookie_samesite', 'Lax');
    ini_set('session.gc_maxlifetime', 3600);
    
    session_start();
}
?>
