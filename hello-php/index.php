<?php
$time = date('g:i A');
$day = date('l');
$quotes = [
    "The best code is no code at all.",
    "First, solve the problem. Then, write the code.",
    "Code is like humor. When you have to explain it, it's bad.",
    "Simplicity is the soul of efficiency.",
    "Talk is cheap. Show me the code.",
];
$quote = $quotes[array_rand($quotes)];
$uptime = shell_exec('uptime -p') ?: 'unknown';
$hostname = gethostname();
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Hello from <?= $hostname ?></title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%);
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            color: #e2e8f0;
        }
        .card {
            background: rgba(30, 41, 59, 0.8);
            border: 1px solid #334155;
            border-radius: 16px;
            padding: 3rem;
            max-width: 500px;
            text-align: center;
            backdrop-filter: blur(10px);
            box-shadow: 0 25px 50px rgba(0,0,0,0.4);
        }
        .pulse {
            width: 12px;
            height: 12px;
            background: #10b981;
            border-radius: 50%;
            display: inline-block;
            margin-right: 8px;
            animation: pulse 2s infinite;
        }
        @keyframes pulse {
            0%, 100% { opacity: 1; transform: scale(1); }
            50% { opacity: 0.5; transform: scale(1.2); }
        }
        h1 { font-size: 2rem; margin-bottom: 0.5rem; }
        .status { color: #10b981; font-size: 0.9rem; margin-bottom: 1.5rem; }
        .quote {
            font-style: italic;
            color: #94a3b8;
            border-left: 3px solid #3b82f6;
            padding-left: 1rem;
            margin: 1.5rem 0;
            text-align: left;
        }
        .info {
            font-size: 0.85rem;
            color: #64748b;
            margin-top: 1.5rem;
            line-height: 1.8;
        }
        .info span { color: #cbd5e1; }
    </style>
</head>
<body>
    <div class="card">
        <h1>Hello, World!</h1>
        <p class="status"><span class="pulse"></span>Server is alive</p>
        <div class="quote">"<?= htmlspecialchars($quote) ?>"</div>
        <div class="info">
            <div>Host: <span><?= htmlspecialchars($hostname) ?></span></div>
            <div>Time: <span><?= $time ?> on <?= $day ?></span></div>
            <div>PHP: <span><?= phpversion() ?></span></div>
            <div>Uptime: <span><?= trim($uptime) ?></span></div>
        </div>
    </div>
</body>
</html>
