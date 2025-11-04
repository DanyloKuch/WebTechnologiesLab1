const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');

let bird;
let pipes = [];
let score = 0;
let gameInterval; 
let frame = 0;

const gravity = 0.5;
const jumpStrength = -8;
const pipeSpeed = 3;
const pipeGap = 150; 
const pipeSpawnRate = 100;

function setupBird() {
    bird = {
        x: 50,
        y: canvas.height / 2,
        width: 30,
        height: 30,
        velocity: 0
    };
}

document.addEventListener('keydown', function (e) {
    if (e.code === 'Space') {
        bird.velocity = jumpStrength; 
    }
});


function update() {
    bird.velocity += gravity; 
    bird.y += bird.velocity; 

    frame++;
    if (frame % pipeSpawnRate === 0) {
        const topHeight = Math.random() * (canvas.height - pipeGap - 100) + 50;
        pipes.push({
            x: canvas.width,
            topHeight: topHeight,
            bottomY: topHeight + pipeGap
        });
    }

    for (let i = pipes.length - 1; i >= 0; i--) {
        let p = pipes[i];
        p.x -= pipeSpeed;

        if (bird.x < p.x + 50 && 
            bird.x + bird.width > p.x &&
            (bird.y < p.topHeight || bird.y + bird.height > p.bottomY)) {
            gameOver();
            return;
        }

        if (p.x + 50 < bird.x && !p.passed) {
            score++;
            p.passed = true;
        }

        if (p.x + 50 < 0) {
            pipes.splice(i, 1);
        }
    }

    if (bird.y + bird.height > canvas.height || bird.y < 0) {
        gameOver();
        return;
    }
}

function draw() {
    ctx.fillStyle = '#70c5ce';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = 'yellow';
    ctx.fillRect(bird.x, bird.y, bird.width, bird.height);

    ctx.fillStyle = '#008000'; 
    for (let p of pipes) {
        ctx.fillRect(p.x, 0, 50, p.topHeight);
        ctx.fillRect(p.x, p.bottomY, 50, canvas.height - p.bottomY);
    }
    
    ctx.fillStyle = 'white';
    ctx.font = '30px Arial';
    ctx.fillText("Score: " + score, 10, 40);
}

function startGame() {
    setupBird();
    pipes = [];
    score = 0;
    frame = 0;

    gameInterval = setInterval(() => {
        update();
        draw();
    }, 1000 / 60);
}

function gameOver() {
    clearInterval(gameInterval); 
    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = 'white';
    ctx.font = '40px Arial';
    ctx.fillText('GAME OVER', canvas.width / 2 - 100, canvas.height / 2 - 40);
    ctx.font = '20px Arial';
    ctx.fillText('Your Score: ' + score, canvas.width / 2 - 50, canvas.height / 2);
    ctx.font = '16px Arial';
    ctx.fillText('Press SPACE to restart', canvas.width / 2 - 70, canvas.height / 2 + 40);

    document.addEventListener('keydown', restartGameOnSpace, { once: true });
}

function restartGameOnSpace(e) {
    if (e.code === 'Space') {
        startGame();
    }
}

startGame();