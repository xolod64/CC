<?php
header('Content-Type: application/json');

// Підключення до БД
$conn = new mysqli('localhost', 'root', '', 'game_server');
if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Помилка підключення до БД']);
    exit;
}

// Отримання даних
$input = json_decode(file_get_contents('php://input'), true);
if (!$input || !isset($input['player_id'])) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'Не вказано player_id']);
    exit;
}

// Перевірка поточного стану гри
$game_state = $conn->query("SELECT * FROM game_state LIMIT 1")->fetch_assoc();
if (!$game_state) {
    // Якщо гра ще не почалась, ініціалізуємо
    $conn->query("INSERT INTO game_state (current_round, is_round_active) VALUES (1, 1)");
    $game_state = ['current_round' => 1, 'is_round_active' => 1];
}

// Збереження ходу
$stmt = $conn->prepare("
    INSERT INTO player_moves 
    (player_id, round, kronus, lyrion, mystara, eclipsia, fiora) 
    VALUES (?, ?, ?, ?, ?, ?, ?)
    ON DUPLICATE KEY UPDATE 
        kronus = VALUES(kronus),
        lyrion = VALUES(lyrion),
        mystara = VALUES(mystara),
        eclipsia = VALUES(eclipsia),
        fiora = VALUES(fiora)
");

$stmt->bind_param("iiiiiii", 
    $input['player_id'],
    $game_state['current_round'],
    $input['kronus'],
    $input['lyrion'],
    $input['mystara'],
    $input['eclipsia'],
    $input['fiora']
);

if (!$stmt->execute()) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Помилка збереження ходу']);
    exit;
}

// Перевірка чи всі зробили хід
$total_players = $conn->query("SELECT COUNT(DISTINCT player_id) as cnt FROM players")->fetch_assoc()['cnt'];
$moves_count = $conn->query("SELECT COUNT(DISTINCT player_id) as cnt FROM player_moves WHERE round = ".$game_state['current_round'])->fetch_assoc()['cnt'];

$response = ['success' => true, 'message' => 'Progress saved successfully'];

if ($moves_count >= $total_players) {
    // Всі зробили хід - починаємо підрахунок
    $conn->query("UPDATE game_state SET is_round_active = 0");
    $response['round_completed'] = true;
    $response['round'] = $game_state['current_round'];
} else {
    $response['waiting_players'] = $total_players - $moves_count;
}

echo json_encode($response);

$stmt->close();
$conn->close();
?>