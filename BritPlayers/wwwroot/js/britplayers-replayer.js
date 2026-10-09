(function () {
    function renderBoard(boardEl, fen) {
        const rows = [8, 7, 6, 5, 4, 3, 2, 1];
        const files = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
        const board = new Chess(fen).board();
        boardEl.innerHTML = '';

        rows.forEach((rank) => {
            files.forEach((file, fileIndex) => {
                const square = `${file}${rank}`;
                const piece = board[8 - rank][fileIndex];
                const squareEl = document.createElement('div');
                const isLight = (rank + fileIndex) % 2 === 0;
                squareEl.className = `chess-square ${isLight ? 'light' : 'dark'}`;
                squareEl.dataset.square = square;
                if (piece) {
                    squareEl.textContent = pieceGlyph(piece.color === 'w' ? piece.type.toUpperCase() : piece.type.toLowerCase());
                }
                boardEl.appendChild(squareEl);
            });
        });
    }

    function pieceGlyph(type) {
        const glyphs = {
            P: '♙', N: '♘', B: '♗', R: '♖', Q: '♕', K: '♔',
            p: '♟', n: '♞', b: '♝', r: '♜', q: '♛', k: '♚'
        };
        return glyphs[type] || '';
    }

    function buildMoveTable(history, tbody, onMoveSelect) {
        tbody.innerHTML = '';
        for (let i = 0; i < history.length; i += 2) {
            const moveNum = Math.floor(i / 2) + 1;
            const whiteMove = history[i];
            const blackMove = history[i + 1];
            const row = document.createElement('tr');

            const numCell = document.createElement('td');
            numCell.className = 'britplayers-move-number';
            numCell.textContent = moveNum;
            row.appendChild(numCell);

            const whiteCell = document.createElement('td');
            whiteCell.className = 'britplayers-move-cell';
            whiteCell.dataset.moveIndex = (i + 1).toString();
            whiteCell.textContent = whiteMove ? whiteMove.san : '';
            if (!whiteMove) whiteCell.classList.add('empty');
            whiteCell.addEventListener('click', () => onMoveSelect(i + 1));
            row.appendChild(whiteCell);

            const blackCell = document.createElement('td');
            blackCell.className = 'britplayers-move-cell';
            if (blackMove) {
                blackCell.dataset.moveIndex = (i + 2).toString();
                blackCell.textContent = blackMove.san;
                blackCell.addEventListener('click', () => onMoveSelect(i + 2));
            } else {
                blackCell.classList.add('empty');
            }
            row.appendChild(blackCell);

            tbody.appendChild(row);
        }
    }

    function bindGameReplayer() {
        const gamePanel = document.getElementById('game-panel');
        if (!gamePanel) return;

        const pgnText = gamePanel.dataset.pgn || '';
        if (!pgnText) return;

        try {
            const game = new Chess();
            game.load_pgn(pgnText, { sloppy: true });
            const history = game.history({ verbose: true });
            const fens = [game.fen()];
            for (let i = 0; i < history.length; i++) {
                game.move(history[i]);
                fens.push(game.fen());
            }

            const boardEl = document.getElementById('game-board');
            const tbody = document.getElementById('game-moves-body');
            if (!boardEl || !tbody) return;

            let currentMoveIndex = 0;

            function updateBoard() {
                renderBoard(boardEl, fens[currentMoveIndex]);
                document.querySelectorAll('.britplayers-move-cell').forEach((el) => {
                    const idx = Number(el.dataset.moveIndex || 0);
                    el.classList.toggle('active', idx === currentMoveIndex);
                });
            }

            buildMoveTable(history, tbody, (index) => {
                currentMoveIndex = Math.max(0, Math.min(index, fens.length - 1));
                updateBoard();
            });

            const startBtn = document.getElementById('replay-start');
            const prevBtn = document.getElementById('replay-prev');
            const nextBtn = document.getElementById('replay-next');
            const endBtn = document.getElementById('replay-end');
            const flipBtn = document.getElementById('replay-flip');

            let flipped = false;
            function renderCurrentBoard() {
                const boardPosition = fens[currentMoveIndex];
                const cfg = new Chess(boardPosition);
                const board = cfg.board();
                const rows = flipped ? [1, 2, 3, 4, 5, 6, 7, 8] : [8, 7, 6, 5, 4, 3, 2, 1];
                const files = flipped ? ['h', 'g', 'f', 'e', 'd', 'c', 'b', 'a'] : ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
                boardEl.innerHTML = '';

                rows.forEach((rank) => {
                    files.forEach((file, fileIndex) => {
                        const square = `${file}${rank}`;
                        const piece = board[flipped ? (rank - 1) : (8 - rank)][fileIndex];
                        const squareEl = document.createElement('div');
                        const isLight = ((flipped ? 7 - fileIndex : fileIndex) + rank) % 2 === 0;
                        squareEl.className = `chess-square ${isLight ? 'light' : 'dark'}`;
                        squareEl.dataset.square = square;
                        if (piece) {
                            squareEl.textContent = pieceGlyph(piece.color === 'w' ? piece.type.toUpperCase() : piece.type.toLowerCase());
                        }
                        boardEl.appendChild(squareEl);
                    });
                });

                document.querySelectorAll('.britplayers-move-cell').forEach((el) => {
                    const idx = Number(el.dataset.moveIndex || 0);
                    el.classList.toggle('active', idx === currentMoveIndex);
                });
            }

            function setMove(index) {
                currentMoveIndex = Math.max(0, Math.min(index, fens.length - 1));
                renderCurrentBoard();
            }

            startBtn?.addEventListener('click', () => setMove(0));
            prevBtn?.addEventListener('click', () => setMove(currentMoveIndex - 1));
            nextBtn?.addEventListener('click', () => setMove(currentMoveIndex + 1));
            endBtn?.addEventListener('click', () => setMove(fens.length - 1));
            flipBtn?.addEventListener('click', () => {
                flipped = !flipped;
                renderCurrentBoard();
            });

            document.addEventListener('keydown', (event) => {
                if (event.key === 'ArrowLeft') setMove(currentMoveIndex - 1);
                if (event.key === 'ArrowRight') setMove(currentMoveIndex + 1);
            });

            renderCurrentBoard();
        } catch (err) {
            console.error('Unable to render PGN board', err);
            boardEl.innerHTML = '<div class="alert alert-warning mb-0">Unable to display this chess game.</div>';
        }
    }

    document.addEventListener('DOMContentLoaded', bindGameReplayer);
})();
