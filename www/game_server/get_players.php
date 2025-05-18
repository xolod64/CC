<?php
header('Content-Type: application/json');

// Параметри підключення до БД
$host = 'localhost';
$user = 'root';
$password = ''; // якщо є — впиши
$dbname = 'game_server'; // або актуальна назва БД

$conn = new mysqli($host, $user, $password, $dbname);

if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(['error' => 'Помилка підключення до БД']);
    exit;
}

// Отримати всіх гравців
$sql = "SELECT id, username, status FROM players";
$result = $conn->query($sql);

$players = [];

if ($result && $result->num_rows > 0) {
    while ($row = $result->fetch_assoc()) {
        $players[] = $row;
    }
}

echo json_encode(['players' => $players]);

$conn->close();
?>
