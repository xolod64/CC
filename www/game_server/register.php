<?php
// Параметри підключення
$db_host = 'localhost';
$db_user = 'root';
$db_pass = '';
$db_name = 'game_server';

$conn = new mysqli($db_host, $db_user, $db_pass, $db_name);
if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(['error' => 'Помилка підключення до бази даних']);
    exit;
}
$conn->set_charset("utf8mb4");

// Перевіряємо, чи є вже гра в процесі
$stmt = $conn->prepare("SELECT COUNT(*) as cnt FROM players WHERE status = 'playing'");
$stmt->execute();
$result = $stmt->get_result();
$row = $result->fetch_assoc();
$playing_count = intval($row['cnt']);
$stmt->close();

if ($playing_count > 0) {
    echo json_encode(['error' => 'Гра вже йде. Нові гравці не можуть приєднатись']);
    $conn->close();
    exit;
}

// Ліміт гравців
$max_players = 5;
$stmt = $conn->prepare("SELECT COUNT(*) as cnt FROM players WHERE status = 'waiting'");
$stmt->execute();
$result = $stmt->get_result();
$row = $result->fetch_assoc();
$waiting_count = intval($row['cnt']);
$stmt->close();

if ($waiting_count >= $max_players) {
    echo json_encode(['error' => "Лоббі заповнене. Максимальна кількість гравців: $max_players"]);
    $conn->close();
    exit;
}

// Отримуємо JSON-запит
$input = json_decode(file_get_contents('php://input'), true);
$username = $input['username'] ?? null;

if (!$username) {
    echo json_encode(['error' => "Не вказано username"]);
    $conn->close();
    exit;
}

// Перевірка унікальності username
$stmt = $conn->prepare("SELECT id FROM players WHERE username = ?");
$stmt->bind_param("s", $username);
$stmt->execute();
$result = $stmt->get_result();

if ($result->num_rows > 0) {
    echo json_encode(['error' => "Ім’я користувача вже зайняте"]);
    $stmt->close();
    $conn->close();
    exit;
}
$stmt->close();

// Додаємо нового гравця
$stmt = $conn->prepare("INSERT INTO players (username, status) VALUES (?, 'waiting')");
$stmt->bind_param("s", $username);
$stmt->execute();

if ($stmt->affected_rows > 0) {
    echo json_encode(['success' => true, 'player_id' => $stmt->insert_id]);
} else {
    echo json_encode(['error' => 'Не вдалося додати гравця']);
}

$stmt->close();
$conn->close();
?>
