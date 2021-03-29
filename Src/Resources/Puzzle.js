window.onload = (function()
{
    function remoteLog(msg)
    {
        //let req = new XMLHttpRequest();
        //req.open('POST', '/remote-log', true);
        //req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        //req.send(`msg=${encodeURIComponent(msg)}`);
    }

    function inRange(x) { return x >= 0 && x < 9; }
    function dx(dir) { return dir === 'Left' ? -1 : dir === 'Right' ? 1 : 0 }
    function dy(dir) { return dir === 'Up' ? -1 : dir === 'Down' ? 1 : 0 }
    function Adjacent(cell)
    {
        let list = [];
        let x = cell % 9;
        let y = (cell / 9) | 0;
        for (let xx = x - 1; xx <= x + 1; xx++)
            if (inRange(xx))
                for (let yy = y - 1; yy <= y + 1; yy++)
                    if (inRange(yy) && (xx != x || yy != y))
                        list.push(xx + 9 * yy);
        return list;
    }

    function Orthogonal(cell)
    {
        let list = [];
        let x = cell % 9;
        let y = (cell / 9) | 0;
        for (let xx = x - 1; xx <= x + 1; xx++)
            if (inRange(xx))
                for (let yy = y - 1; yy <= y + 1; yy++)
                    if (inRange(yy) && (xx == x || yy == y) && (xx != x || yy != y))
                        list.push(xx + 9 * yy);
        return list;
    }

    function validateConstraint(grid, constr)
    {
        switch (constr[':type'])
        {
            // CELL CONSTRAINTS

            case 'OddEven':
                return grid[constr.Cell] === null ? null : grid[constr.Cell] % 2 === (constr.Odd ? 1 : 0);

            case 'AntiBishop': {
                if (grid[constr.Cell] === null)
                    return null;
                let diagonals = Array(81).fill(null).map((_, c) => c).filter(c => c != constr.Cell && Math.abs(c % 9 - constr.Cell % 9) === Math.abs(((c / 9) | 0) - ((constr.Cell / 9) | 0)));
                return diagonals.some(c => grid[c] !== null && grid[c] === grid[constr.Cell]) ? false :
                    diagonals.some(c => grid[c] === null) ? null : true;
            }

            case 'AntiKnight': {
                let x = constr.Cell % 9;
                let y = (constr.Cell / 9) | 0;
                let knightsMoves = [];
                for (let dx of [-2, -1, 1, 2])
                    if (inRange(x + dx))
                        for (let dy of (dx === 1 || dx === -1) ? [-2, 2] : [-1, 1])
                            if (inRange(y + dy))
                                knightsMoves.push(x + dx + 9 * (y + dy));
                return knightsMoves.some(c => grid[c] !== null && grid[c] === grid[constr.Cell]) ? false :
                    knightsMoves.some(c => grid[c] === null) ? null : true;
            }

            case 'AntiKing':
                return Adjacent(constr.Cell).some(c => grid[c] !== null && grid[c] === grid[constr.Cell]) ? false :
                    Adjacent(constr.Cell).some(c => grid[c] === null) ? null : true;

            case 'NoConsecutive':
                return grid[constr.Cell] !== null && Orthogonal(constr.Cell).some(c => grid[c] !== null && Math.abs(grid[c] - grid[constr.Cell]) === 1) ? false :
                    grid[constr.Cell] === null || Orthogonal(constr.Cell).some(c => grid[c] === null) ? null : true;

            case 'MaximumCell':
                return grid[constr.Cell] !== null && Orthogonal(constr.Cell).some(c => grid[c] !== null && grid[c] >= grid[constr.Cell]) ? false :
                    grid[constr.Cell] === null || Orthogonal(constr.Cell).some(c => grid[c] === null) ? null : true;

            case 'FindThe9':
                return grid[constr.Cell] === null || grid[constr.Cell + (dx(constr.Direction) + 9 * dy(constr.Direction)) * grid[constr.Cell]] === null ? null :
                    grid[constr.Cell + (dx(constr.Direction) + 9 * dy(constr.Direction)) * grid[constr.Cell]] === 9;


            // ROW/COLUMN CONSTRAINTS

            case 'Sandwich': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * x) : (x + 9 * constr.RowCol)]);
                let p1 = numbers.indexOf(constr.Digit1);
                let p2 = numbers.indexOf(constr.Digit2);
                if (p1 === -1 || p2 === -1)
                    return numbers.some(n => n === null) ? null : false;
                let sandwich = numbers.slice(Math.min(p1, p2) + 1, Math.max(p1, p2));
                return sandwich.some(n => n === null) ? null : sandwich.reduce((p, n) => p + n, 0) === constr.Clue;
            }

            case 'ToroidalSandwich': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * x) : (x + 9 * constr.RowCol)]);
                let p1 = numbers.indexOf(constr.Digit1);
                let p2 = numbers.indexOf(constr.Digit2);
                if (p1 === -1 || p2 === -1)
                    return numbers.some(n => n === null) ? null : false;
                let s = 0;
                let i = (p1 + 1) % numbers.length;
                while (i !== p2)
                {
                    if (numbers[i] === null)
                        return null;
                    s += numbers[i];
                    i = (i + 1) % numbers.length;
                }
                return s === constr.Clue;
            }

            case 'Skyscraper': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * (constr.Reverse ? 8 - x : x)) : ((constr.Reverse ? 8 - x : x) + 9 * constr.RowCol)]);
                if (numbers.some(n => n === null))
                    return null;
                let c = 0, p = 0;
                for (let n of numbers)
                    if (n > p)
                    {
                        p = n;
                        c++;
                    }
                return c === constr.Clue;
            }

            case 'XSum': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * (constr.Reverse ? 8 - x : x)) : ((constr.Reverse ? 8 - x : x) + 9 * constr.RowCol)]);
                if (numbers[0] === null || numbers.slice(0, numbers[0]).some(n => n === null))
                    return null;
                return constr.Clue === numbers.slice(0, numbers[0]).reduce((p, n) => p + n, 0);
            }

            case 'Battlefield': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * x) : (x + 9 * constr.RowCol)]);
                if (numbers[0] === null || numbers[8] === null)
                    return null;
                let left = numbers[0];
                let right = numbers[numbers.length - 1];
                let sum = 0;
                if (numbers.length - left - right >= 0)
                    for (let ix = left; ix < numbers.length - right; ix++)
                    {
                        if (numbers[ix] === null)
                            return null;
                        sum += numbers[ix];
                    }
                else
                    for (let ix = numbers.length - right; ix < left; ix++)
                    {
                        if (numbers[ix] === null)
                            return null;
                        sum += numbers[ix];
                    }
                return sum === constr.Clue;
            }

            case 'Binairo': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * x) : (x + 9 * constr.RowCol)]);
                for (let i = 1; i < numbers.length - 1; i++)
                    if (numbers[i - 1] !== null && numbers[i] !== null && numbers[i + 1] !== null && numbers[i - 1] % 2 === numbers[i] % 2 && numbers[i + 1] % 2 === numbers[i] % 2)
                        return false;
                return numbers.some(n => n === null) ? null : true;
            }

            // REGION CONSTRAINTS

            case 'Thermometer': {
                for (let i = 0; i < constr.Cells.length; i++)
                    for (let j = i + 1; j < constr.Cells.length; j++)
                        if (grid[constr.Cells[i]] !== null && grid[constr.Cells[j]] !== null && grid[constr.Cells[i]] >= grid[constr.Cells[j]])
                            return false;
                return constr.Cells.some(c => grid[c] === null) ? null : true;
            }

            case 'Arrow':
                return constr.Cells.some(c => grid[c] === null) ? null : grid[constr.Cells[0]] === constr.Cells.slice(1).reduce((sum, cell) => sum + grid[cell], 0);

            case 'Palindrome':
                for (let i = 0; i < (constr.Cells.length / 2) | 0; i++)
                    if (grid[constr.Cells[i]] !== null && grid[constr.Cells[constr.Cells.length - 1 - i]] !== null && grid[constr.Cells[i]] !== grid[constr.Cells[constr.Cells.length - 1 - i]])
                        return false;
                return constr.Cells.some(c => grid[c] === null) ? null : true;

            case 'KillerCage': {
                for (let i = 0; i < constr.Cells.length; i++)
                    for (let j = i + 1; j < constr.Cells.length; j++)
                        if (grid[constr.Cells[i]] !== null && grid[constr.Cells[j]] !== null && grid[constr.Cells[i]] === grid[constr.Cells[j]])
                            return false;
                return constr.Cells.some(c => grid[c] === null) ? null : (constr.Sum === null || constr.Cells.reduce((p, n) => p + grid[n], 0) === constr.Sum);
            }

            case 'RenbanCage': {
                let numbers = constr.Cells.map(c => grid[c]);
                return numbers.some(n => n === null) ? null : numbers.filter(n => !numbers.includes(n + 1)).length === 1;
            }

            case 'Snowball': {
                let offsets = [...new Set(Array(constr.Cells1.length).fill(null).map((_, ix) => grid[constr.Cells1[ix]] === null || grid[constr.Cells2[ix]] === null ? null : grid[constr.Cells2[ix]] - grid[constr.Cells1[ix]]).filter(c => c !== null))];
                return offsets.length > 1 ? false : constr.Cells1.some(n => grid[n] === null) || constr.Cells2.some(n => grid[n] === null) ? null : true;
            }

            // FOUR-CELL CONSTRAINTS

            case 'Clockface': {
                let numbers = [0, 1, 10, 9].map(o => grid[constr.TopLeftCell + o]);
                if (numbers.some(n => n === null))
                    return null;
                let a = numbers[0], b = numbers[1], c = numbers[2], d = numbers[3];
                return constr.Clockwise
                    ? (a < b && b < c && c < d) || (b < c && c < d && d < a) || (c < d && d < a && a < b) || (d < a && a < b && b < c)
                    : (a > b && b > c && c > d) || (b > c && c > d && d > a) || (c > d && d > a && a > b) || (d > a && a > b && b > c);
            }

            case 'Inclusion': {
                let numbers = [0, 1, 10, 9].map(o => grid[constr.TopLeftCell + o]);
                if (numbers.some(n => n === null))
                    return null;
                return constr.Digits.every(d => numbers.filter(n => n === d).length >= constr.Digits.filter(d2 => d2 === d).length);
            }

            case 'Battenburg': {
                let offsets = [0, 1, 10, 9].map(c => constr.TopLeftCell + c);
                for (let i = 0; i < 4; i++)
                    if (grid[offsets[i]] !== null && grid[offsets[(i + 1) % offsets.length]] !== null && grid[offsets[i]] % 2 === grid[offsets[(i + 1) % offsets.length]] % 2)
                        return false;
                return offsets.some(c => grid[c] === null) ? null : true;
            }

            // OTHER CONSTRAINTS

            case 'ConsecutiveNeighbors':
                return grid[constr.Cell1] === null || grid[constr.Cell2] === null ? null : Math.abs(grid[constr.Cell1] - grid[constr.Cell2]) === 1;

            case 'DoubleNeighbors':
                return grid[constr.Cell1] === null || grid[constr.Cell2] === null ? null : grid[constr.Cell1] * 2 === grid[constr.Cell2] || grid[constr.Cell2] * 2 === grid[constr.Cell1];

            case 'LittleKiller': {
                let affectedCells = [];
                switch (constr.Direction)
                {
                    case 'SouthEast': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => constr.Offset + 10 * i); break;
                    case 'SouthWest': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => 8 + 9 * constr.Offset + 8 * i); break;
                    case 'NorthWest': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => 80 - constr.Offset - 10 * i); break;
                    case 'NorthEast': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => 72 - 9 * constr.Offset - 8 * i); break;
                };
                return affectedCells.some(c => grid[c] === null) ? null : affectedCells.reduce((p, n) => p + grid[n], 0) === constr.Sum;
            }


            // EXOTIC CONSTRAINTS

            case 'YSum': {
                let numbers = Array(9).fill(null).map((_, x) => grid[constr.IsCol ? (constr.RowCol + 9 * (constr.Reverse ? 8 - x : x)) : ((constr.Reverse ? 8 - x : x) + 9 * constr.RowCol)]);
                if (numbers[0] === null || numbers[numbers[0] - 1] === null || numbers.slice(0, numbers[numbers[0] - 1]).some(n => n === null))
                    return null;
                return constr.Clue === numbers.slice(0, numbers[numbers[0] - 1]).reduce((p, n) => p + n, 0);
            }

            case 'BinairoCage': {
                let anyNull = false;
                function verifyRowOrCol(isOdd)
                {
                    let arrangements = [];
                    let indexes = Array(constr.Size).fill(null).map((_, c) => c);
                    for (let y = 0; y < constr.Size; y++)
                    {
                        // Check for three equal parities in a row
                        if (Array(constr.Size - 2).fill(null).some((_, x) => isOdd(x, y) !== null && isOdd(x + 1, y) !== null && isOdd(x + 2, y) !== null && isOdd(x, y) === isOdd(x + 1, y) && isOdd(x, y) === isOdd(x + 2, y)))
                            return false;
                        // Check if anything is null
                        if (indexes.some(x => isOdd(x, y) === null))
                        {
                            anyNull = true;
                            continue;
                        }
                        // Check equal odds/evens
                        if (indexes.filter(x => isOdd(x, y)).length * 2 !== constr.Size)
                            return false;
                        // Check repeated arrangements of odds/evens
                        let arrangement = indexes.map(x => isOdd(x, y)).reduce((p, n) => (p << 1) | (n ? 1 : 0), 0);
                        if (arrangements.includes(arrangement))
                            return false;
                        arrangements.push(arrangement);
                    }
                    return true;
                }
                let ox = constr.TopLeft % 9;
                let oy = (constr.TopLeft / 9) | 0;
                let e = verifyRowOrCol((i, j) => grid[i + ox + 9 * (j + oy)] === null ? null : grid[i + ox + 9 * (j + oy)] % 2 !== 0)
                    && verifyRowOrCol((i, j) => grid[j + ox + 9 * (i + oy)] === null ? null : grid[j + ox + 9 * (i + oy)] % 2 !== 0);
                return anyNull && e ? null : e;
            }

            case 'LittleSandwich': {
                let affectedCells = [];
                switch (constr.Direction)
                {
                    case 'SouthEast': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => constr.Offset + 10 * i); break;
                    case 'SouthWest': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => 8 + 9 * constr.Offset + 8 * i); break;
                    case 'NorthWest': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => 80 - constr.Offset - 10 * i); break;
                    case 'NorthEast': affectedCells = Array(9 - constr.Offset).fill(null).map((_, i) => 72 - 9 * constr.Offset - 8 * i); break;
                };
                let p1 = affectedCells.findIndex(ix => grid[ix] == constr.Digit1);
                let p2 = affectedCells.findIndex(ix => grid[ix] == constr.Digit2);
                if (p1 == -1 || p2 == -1)
                    return affectedCells.some(c => grid[c] === null) ? null : false;
                let slice = affectedCells.slice(Math.min(p1, p2) + 1, Math.max(p1, p2));
                console.log(`${p1}, ${p2}, [${affectedCells.join(', ')}], [${slice.join(', ')}]=[${slice.map(ix => grid[ix]).join(', ')}]`);
                return slice.some(c => grid[c] === null) ? null : slice.reduce((p, n) => p + grid[n], 0) === constr.Clue;
            }
        }
    }

    function handler(fnc)
    {
        return function(ev)
        {
            fnc(ev);
            ev.stopPropagation();
            ev.preventDefault();
            return false;
        };
    }

    function setClass(elem, className, setUnset)
    {
        if (setUnset)
            elem.classList.add(className);
        else
            elem.classList.remove(className);
    }

    let first = true;
    let draggingMode = null;
    document.body.onmouseup = handler(document.body.ontouchend = function(ev)
    {
        if (ev.type !== 'touchend' || ev.touches.length === 0)
            draggingMode = null;
        remoteLog(`${ev.type} document.body null`);
    });

    Array.from(document.getElementsByClassName('puzzle')).forEach(puzzleDiv =>
    {
        let puzzleId = puzzleDiv.dataset.puzzleid | 0;
        let constraints = JSON.parse(puzzleDiv.dataset.constraints || null) || [];

        if (first)
        {
            puzzleDiv.focus();
            first = false;
        }

        let state = {
            colors: Array(81).fill(null),
            cornerNotation: Array(81).fill(null).map(_ => []),
            centerNotation: Array(81).fill(null).map(_ => []),
            enteredDigits: Array(81).fill(null)
        };
        let undoBuffer = [JSON.parse(JSON.stringify(state))];
        let redoBuffer = [];

        let mode = 'normal';
        let selectedCells = [];
        let highlightedDigits = [];
        let showErrors = 1;
        let sidebarMode = 'off';

        function remoteLog2(msg)
        {
            remoteLog(`${msg} [${selectedCells.join()}] ${draggingMode ?? "null"}`);
        }

        function encodeState(st)
        {
            let val = 0n;

            // Encode the Sudoku grid
            for (let cell = 0; cell < 81; cell++)
            {
                // Compact representation of an entered digit or a completely empty cell
                if (st.enteredDigits[cell] !== null)
                    val = (val * 11n) + BigInt(st.enteredDigits[cell]);
                else if (st.cornerNotation[cell].length === 0 && st.centerNotation[cell].length === 0)
                    val = (val * 11n);
                else
                {
                    // corner notation
                    for (let digit = 1; digit <= 9; digit++)
                        val = (val * 2n) + (st.cornerNotation[cell].includes(digit) ? 1n : 0n);

                    // center notation
                    for (let digit = 1; digit <= 9; digit++)
                        val = (val * 2n) + (st.centerNotation[cell].includes(digit) ? 1n : 0n);

                    val = (val * 11n) + 10n;
                }

                if (st.colors[cell] === null)
                    val = (val * 2n);
                else
                    val = (val * 9n + BigInt(st.colors[cell])) * 2n + 1n;
            }

            // Safe characters to use: 0x21 - 0xD7FF and 0xE000 - 0xFFFD
            // (0x20 will later be used as a separator)
            let maxValue = BigInt(0xfffd - 0xe000 + 1 + 0xd7ff - 0x21 + 1);
            function getChar(v) { return String.fromCharCode(v > 0xd7ff - 0x21 + 1 ? 0xe000 + (v - (0xd7ff - 0x21 + 1)) : 0x21 + v); }

            let str = '';
            while (val > 0n)
            {
                str += getChar(Number(val % maxValue));
                val = val / maxValue;
            }
            return str;
        }

        function decodeState(str)
        {
            // Safe characters to use: 0x21 - 0xD7FF and 0xE000 - 0xFFFD
            // (0x20 will later be used as a separator)
            let maxValue = BigInt(0xfffd - 0xe000 + 1 + 0xd7ff - 0x21 + 1);
            function charToVal(ch) { return ch >= 0xe000 ? ch - 0xe000 + 0xd7ff - 0x21 + 1 : ch - 0x21; }

            let val = 0n;
            for (let ix = str.length - 1; ix >= 0; ix--)
                val = (val * maxValue) + BigInt(charToVal(str.charCodeAt(ix)));

            let st = {
                colors: Array(81).fill(null),
                cornerNotation: Array(81).fill(null).map(_ => []),
                centerNotation: Array(81).fill(null).map(_ => []),
                enteredDigits: Array(81).fill(null)
            };

            // Decode Sudoku grid
            for (let cell = 81 - 1; cell >= 0; cell--)
            {
                let hasColor = (val % 2n !== 0n);
                val = val / 2n;
                if (hasColor)
                {
                    st.colors[cell] = Number(val % 9n);
                    val = val / 9n;
                }
                else
                    st.colors[cell] = null;

                let code = val % 11n;
                val = val / 11n;
                // Complex case: center notation and corner notation
                if (code === 10n)
                {
                    // Center notation
                    for (let digit = 9; digit >= 1; digit--)
                    {
                        if (val % 2n === 1n)
                            st.centerNotation[cell].unshift(digit);
                        val = val / 2n;
                    }

                    // Corner notation
                    for (let digit = 9; digit >= 1; digit--)
                    {
                        if (val % 2n === 1n)
                            st.cornerNotation[cell].unshift(digit);
                        val = val / 2n;
                    }
                }
                else if (code > 0n)
                    st.enteredDigits[cell] = Number(code);
            }
            return st;
        }

        try
        {
            let undoB = localStorage.getItem(`su${puzzleId}-undo`);
            let redoB = localStorage.getItem(`su${puzzleId}-redo`);

            undoBuffer = undoB ? undoB.split(' ').map(decodeState) : [JSON.parse(JSON.stringify(state))];
            redoBuffer = redoB ? redoB.split(' ').map(decodeState) : [];

            let item = null;
            if (puzzleDiv.dataset.progress)
            {
                item = JSON.parse(puzzleDiv.dataset.progress);
                if (undoB && undoB.includes(encodeState(item)))
                    item = null;
            }

            if (item === null)
            {
                str = localStorage.getItem(`su${puzzleId}`);
                if (str !== null)
                    try { item = JSON.parse(localStorage.getItem(`su${puzzleId}`)); }
                    catch { item = decodeState(str); }
            }
            if (item && item.cornerNotation && item.centerNotation && item.enteredDigits)
                state = item;
        }
        catch
        {
        }

        function resetClearButton()
        {
            document.getElementById(`btn-clear`).classList.remove('warning');
            document.querySelector(`#btn-clear>text`).textContent = 'Clear';
        }

        function getDisplayedSudokuDigit(st, cell)
        {
            return st.enteredDigits[cell];
        }

        function isSudokuValid()
        {
            let grid = Array(81).fill(null).map((_, c) => getDisplayedSudokuDigit(state, c)).map(x => x === false ? null : x);

            // Check the Sudoku rules (rows, columns and regions)
            for (let i = 0; i < 9; i++)
            {
                for (let colA = 0; colA < 9; colA++)
                    for (let colB = colA + 1; colB < 9; colB++)
                        if (grid[colA + 9 * i] !== null && grid[colA + 9 * i] === grid[colB + 9 * i])
                            return false;
                for (let rowA = 0; rowA < 9; rowA++)
                    for (let rowB = rowA + 1; rowB < 9; rowB++)
                        if (grid[i + 9 * rowA] !== null && grid[i + 9 * rowA] === grid[i + 9 * rowB])
                            return false;
                for (let cellA = 0; cellA < 9; cellA++)
                    for (let cellB = cellA + 1; cellB < 9; cellB++)
                        if (grid[cellA % 3 + 3 * (i % 3) + 9 * (((cellA / 3) | 0) + 3 * ((i / 3) | 0))] !== null &&
                            grid[cellA % 3 + 3 * (i % 3) + 9 * (((cellA / 3) | 0) + 3 * ((i / 3) | 0))] === grid[cellB % 3 + 3 * (i % 3) + 9 * (((cellB / 3) | 0) + 3 * ((i / 3) | 0))])
                            return false;
            }

            // Check if any constraints are violated
            for (let constr of constraints)
                if (validateConstraint(grid, constr) === false)
                    return false;

            // Check that all cells in the Sudoku grid have a digit
            return grid.some(c => c === null) ? null : true;
        }

        function updateVisuals(udpateStorage)
        {
            // Update localStorage (only do this when necessary because encodeState() is relatively slow on Firefox)
            if (localStorage && udpateStorage)
            {
                localStorage.setItem(`su${puzzleId}`, encodeState(state));
                localStorage.setItem(`su${puzzleId}-undo`, undoBuffer.map(encodeState).join(' '));
                localStorage.setItem(`su${puzzleId}-redo`, redoBuffer.map(encodeState).join(' '));
            }
            resetClearButton();

            // Check if there are any conflicts (red glow) and/or the puzzle is solved
            let isSolved = true;
            switch (isSudokuValid())
            {
                case false:
                    isSolved = false;
                    if (showErrors)
                        document.getElementById(`sudoku-frame`).classList.add('invalid');
                    break;

                case true:
                    document.getElementById(`sudoku-frame`).classList.remove('invalid');
                    break;

                case null:
                    isSolved = false;
                    document.getElementById(`sudoku-frame`).classList.remove('invalid');
                    break;
            }

            setClass(puzzleDiv, 'solved', isSolved);

            // Sudoku grid (digits, highlights — not red glow, that’s done further up)
            let digitCounts = Array(9).fill(0);
            for (let cell = 0; cell < 81; cell++)
            {
                let digit = getDisplayedSudokuDigit(state, cell);
                digitCounts[digit - 1]++;

                let sudokuCell = document.getElementById(`sudoku-${cell}`);
                setClass(sudokuCell, 'highlighted', selectedCells.includes(cell) || highlightedDigits.includes(digit));
                setClass(sudokuCell, 'invalid', digit === false);

                let intendedText = null;
                let intendedCenterDigits = null;
                let intendedCornerDigits = null;

                if (digit)
                    intendedText = digit;
                else
                {
                    intendedCenterDigits = state.centerNotation[cell].join('');
                    intendedCornerDigits = state.cornerNotation[cell];
                }

                document.getElementById(`sudoku-text-${cell}`).textContent = intendedText !== null ? intendedText : '';
                document.getElementById(`sudoku-center-text-${cell}`).textContent = intendedCenterDigits !== null ? intendedCenterDigits : '';
                for (let i = 0; i < 8; i++)
                    document.getElementById(`sudoku-corner-text-${cell}-${i}`).textContent = intendedCornerDigits !== null && i < intendedCornerDigits.length ? intendedCornerDigits[i] : '';

                for (let color = 0; color < 9; color++)
                    setClass(sudokuCell, `c${color}`, state.colors[cell] === color);
            }

            // Button highlights
            for (let md of ["normal", "center", "corner", "color"])
            {
                setClass(document.getElementById(`btn-${md}`), 'selected', mode === md);
                setClass(puzzleDiv, `mode-${md}`, mode === md);
            }

            for (let digit = 0; digit < 9; digit++)
            {
                setClass(document.getElementById(`btn-${digit + 1}`), 'selected', highlightedDigits.includes(digit + 1));
                setClass(document.getElementById(`btn-${digit + 1}`), 'success', digitCounts[digit] === 9);
            }
        }
        updateVisuals(true);

        function saveUndo()
        {
            undoBuffer.push(JSON.parse(JSON.stringify(state)));
            redoBuffer = [];
        }

        function undo()
        {
            if (undoBuffer.length > 0)
            {
                redoBuffer.push(state);
                let item = undoBuffer.pop();
                state = item;
                updateVisuals(true);
            }
        }

        function redo()
        {
            if (redoBuffer.length > 0)
            {
                undoBuffer.push(state);
                let item = redoBuffer.pop();
                state = item;
                updateVisuals(true);
            }
        }

        function enterCenterNotation(digit)
        {
            if (selectedCells.every(c => getDisplayedSudokuDigit(state, c)))
                return;
            saveUndo();
            let allHaveDigit = selectedCells.filter(c => !getDisplayedSudokuDigit(state, c)).every(c => state.centerNotation[c].includes(digit));
            selectedCells.forEach(cell =>
            {
                if (allHaveDigit)
                    state.centerNotation[cell].splice(state.centerNotation[cell].indexOf(digit), 1);
                else if (!state.centerNotation[cell].includes(digit))
                {
                    state.centerNotation[cell].push(digit);
                    state.centerNotation[cell].sort();
                }
            });
            updateVisuals(true);
        }

        function enterCornerNotation(digit)
        {
            if (selectedCells.every(c => getDisplayedSudokuDigit(state, c)))
                return;
            saveUndo();
            let allHaveDigit = selectedCells.filter(c => !getDisplayedSudokuDigit(state, c)).every(c => state.cornerNotation[c].includes(digit));
            selectedCells.forEach(cell =>
            {
                if (allHaveDigit)
                    state.cornerNotation[cell].splice(state.cornerNotation[cell].indexOf(digit), 1);
                else if (!state.cornerNotation[cell].includes(digit))
                {
                    state.cornerNotation[cell].push(digit);
                    state.cornerNotation[cell].sort();
                }
            });
            updateVisuals(true);
        }

        function setCellColor(color)
        {
            if (selectedCells.every(cell => state.colors[cell] === color))
                return;
            saveUndo();
            for (let cell of selectedCells)
                state.colors[cell] = color;
            updateVisuals(true);
        }

        function pressDigit(digit, ev)
        {
            if (selectedCells.length === 0)
            {
                // Highlight digits
                if (ev && ev.shift)
                {
                    if (highlightedDigits.includes(digit))
                        highlightedDigits.splice(highlightedDigits.indexOf(digit), 1);
                    else
                        highlightedDigits.push(digit);
                }
                else
                {
                    if (highlightedDigits.includes(digit))
                        highlightedDigits = [];
                    else
                        highlightedDigits = [digit];
                }
                updateVisuals();
            }
            else
            {
                // Enter a digit in the Sudoku
                switch (mode)
                {
                    case 'normal':
                        saveUndo();
                        let allHaveDigit = selectedCells.every(c => getDisplayedSudokuDigit(state, c) === digit);
                        if (allHaveDigit)
                            selectedCells.forEach(selectedCell => { state.enteredDigits[selectedCell] = null; });
                        else
                            selectedCells.forEach(selectedCell => { state.enteredDigits[selectedCell] = digit; });
                        updateVisuals(true);
                        break;
                    case 'center':
                        enterCenterNotation(digit);
                        break;
                    case 'corner':
                        enterCornerNotation(digit);
                        break;
                    case 'color':
                        setCellColor(digit - 1);
                        break;
                }
            }
        }

        let tooltip = null;
        function clearTooltip()
        {
            if (tooltip !== null)
            {
                tooltip.parentNode.removeChild(tooltip);
                tooltip = null;
            }
        }

        Array.from(puzzleDiv.getElementsByClassName('sudoku-cell')).forEach(cellRect =>
        {
            let cell = parseInt(cellRect.dataset.cell);
            cellRect.onclick = handler(function() { remoteLog2(`onclick ${cell}`); });
            cellRect.onmousedown = cellRect.ontouchstart = handler(function(ev)
            {
                if (draggingMode !== null)
                {
                    remoteLog2(`${ev.type} ${cell} (canceled)`);
                    return;
                }
                let shift = ev.ctrlKey || ev.shiftKey;
                draggingMode = shift && selectedCells.includes(cell) ? 'remove' : 'add';
                highlightedDigits = [];
                selectCell(cell, shift ? draggingMode : 'toggle');
                updateVisuals();
                remoteLog2(`${ev.type} ${cell} (${ev.x}, ${ev.y})`);
            });
            cellRect.onmousemove = function(ev)
            {
                if (draggingMode === null)
                {
                    remoteLog2(`onmousemove ${cell} (canceled)`);
                    return;
                }
                selectCell(cell, draggingMode);
                updateVisuals();
                remoteLog2(`onmousemove ${cell} (${ev.x}, ${ev.y})`);
            };
            cellRect.ontouchmove = function(ev)
            {
                if (draggingMode === null)
                {
                    remoteLog2(`ontouchmove ${cell} (canceled)`);
                    return;
                }
                let any = false;
                for (let touch of ev.touches)
                {
                    let elem = document.elementFromPoint(touch.pageX, touch.pageY);
                    if (elem && elem.dataset.cell !== undefined)
                    {
                        selectCell(elem.dataset.cell | 0, draggingMode);
                        any = true;
                    }
                }
                if (any)
                    updateVisuals();
                remoteLog2(`ontouchmove ${cell}`);
            };
        });

        Array.from(puzzleDiv.getElementsByClassName('has-tooltip')).forEach(rect =>
        {
            rect.onmouseout = handler(clearTooltip);
            rect.onmouseenter = function()
            {
                if (!rect.dataset.description)
                    return;
                function e(name) { return document.createElementNS('http://www.w3.org/2000/svg', name); }
                tooltip = e('g');
                tooltip.setAttribute('text-anchor', 'middle');
                tooltip.setAttribute('font-size', '.35');
                let y = -.3;
                function makeText(str, isBold, offset)
                {
                    let elem = e('text');
                    elem.textContent = str;
                    if (isBold)
                        elem.setAttribute('font-weight', 'bold');
                    elem.setAttribute('x', '0');
                    elem.setAttribute('y', y);
                    tooltip.appendChild(elem);
                    y += offset;
                    return elem;
                }
                let names = JSON.parse(rect.dataset.name);
                let descrs = JSON.parse(rect.dataset.description);
                for (let cn = 0; cn < names.length; cn++)
                {
                    y += .3;
                    makeText(names[cn], true, .7);
                    let str = descrs[cn];
                    let wordWrapWidth = 55;
                    while (str.length > 0)
                    {
                        let txt = str;
                        if (str.length > wordWrapWidth)
                        {
                            let p = str.lastIndexOf(' ', wordWrapWidth);
                            txt = str.substr(0, p === -1 ? wordWrapWidth : p + 1);
                        }
                        str = str.substr(txt.length).trim();
                        makeText(txt.trim(), false, .5);
                    }
                }
                let tooltipWidth = 9.75;
                let rightEdge = (rect.getAttribute('x') | 0) === 9;
                tooltip.setAttribute('transform', rightEdge
                    ? `translate(${8.7 - tooltipWidth / 2}, ${(rect.getAttribute('y') | 0) + .75})`
                    : `translate(${(rect.getAttribute('x') | 0) - tooltipWidth / 2 + 1.25}, ${(rect.getAttribute('y') | 0) + 2})`);

                let path = e('path');
                path.setAttribute('d', rightEdge ? `m${-tooltipWidth / 2} -.7 ${tooltipWidth} 0 0 .25 .25 .25 -.25 .25 v ${y - .05} h ${-tooltipWidth} z` : `m${-tooltipWidth / 2} -.7 ${tooltipWidth - 1} 0 .25 -.25 .25 .25 .5 0 v ${y + .7} h ${-tooltipWidth} z`);
                path.setAttribute('fill', '#fcedca');
                path.setAttribute('stroke', 'black');
                path.setAttribute('stroke-width', '.025');
                tooltip.insertBefore(path, tooltip.firstChild);

                document.getElementById(`full-puzzle`).appendChild(tooltip);
            };
        });

        function setButtonHandler(btn, click)
        {
            btn.onclick = handler(ev => click(ev));
            btn.onmousedown = handler(function() { });
        }

        Array(9).fill(null).forEach((_, btn) =>
        {
            setButtonHandler(document.getElementById(`btn-${btn + 1}`), function(ev) { pressDigit(btn + 1, ev); });
        });

        ["normal", "corner", "center", "color"].forEach(btn => setButtonHandler(puzzleDiv.querySelector(`#btn-${btn}>rect`), function()
        {
            mode = btn;
            updateVisuals();
        }));

        function clearCells()
        {
            if (mode === 'color' && selectedCells.some(c => state.colors[c] !== null))
            {
                saveUndo();
                for (let cell of selectedCells)
                    state.colors[cell] = null;
                updateVisuals(true);
            }
            else if (mode !== 'color' && selectedCells.some(c => state.enteredDigits[c] !== null || state.centerNotation[c].length > 0 || state.cornerNotation[c].length > 0))
            {
                saveUndo();
                for (let cell of selectedCells)
                {
                    state.enteredDigits[cell] = null;
                    state.centerNotation[cell] = [];
                    state.cornerNotation[cell] = [];
                }
                updateVisuals(true);
            }
        }

        setButtonHandler(puzzleDiv.querySelector(`#btn-clear>rect`), function()
        {
            let elem = document.getElementById(`btn-clear`);
            if (!elem.classList.contains('warning'))
            {
                clearCells();
                elem.classList.add('warning');
                puzzleDiv.querySelector(`#btn-clear>text`).textContent = 'Restart';
            }
            else
            {
                resetClearButton();
                saveUndo();
                state = {
                    colors: Array(81).fill(null),
                    cornerNotation: Array(81).fill(null).map(_ => []),
                    centerNotation: Array(81).fill(null).map(_ => []),
                    enteredDigits: Array(81).fill(null)
                };
                updateVisuals(true);
            }
        });

        setButtonHandler(puzzleDiv.querySelector(`#btn-undo>rect`), undo);
        setButtonHandler(puzzleDiv.querySelector(`#btn-redo>rect`), redo);

        function selectCell(cell, mode)
        {
            if (mode === 'toggle')
            {
                if (selectedCells.length === 1 && selectedCells.includes(cell))
                    selectedCells = [];
                else
                    selectedCells = [cell];
            }
            else if (mode === 'remove')
            {
                let ix = selectedCells.indexOf(cell);
                if (ix !== -1)
                    selectedCells.splice(ix, 1);
            }
            else if (mode === 'clear')
            {
                selectedCells = [cell];
                keepMove = false;
            }
            else if (mode === 'add' || (mode === 'move' && keepMove))
            {
                let ix = selectedCells.indexOf(cell);
                if (ix !== -1)
                    selectedCells.splice(ix, 1);
                selectedCells.push(cell);
                keepMove = false;
            }
            else    // mode === 'move' && !keepMove
            {
                selectedCells.pop();
                selectedCells.push(cell);
            }
        }

        let keepMove = false;
        puzzleDiv.addEventListener("keydown", ev =>
        {
            let str = ev.code;
            if (ev.shiftKey)
                str = `Shift+${str}`;
            if (ev.altKey)
                str = `Alt+${str}`;
            if (ev.ctrlKey)
                str = `Ctrl+${str}`;

            let anyFunction = true;

            function ArrowMovement(dx, dy, mode)
            {
                highlightedDigits = [];
                if (selectedCells.length === 0)
                    selectedCells = [0];
                else
                {
                    let lastCell = selectedCells[selectedCells.length - 1];
                    let newX = ((lastCell % 9) + 9 + dx) % 9;
                    let newY = (((lastCell / 9) | 0) + 9 + dy) % 9;
                    let coord = newX + 9 * newY;
                    selectCell(coord, mode);
                }
                updateVisuals();
            }

            switch (str)
            {
                // Keys that change something
                case 'Digit1': case 'Numpad1':
                case 'Digit2': case 'Numpad2':
                case 'Digit3': case 'Numpad3':
                case 'Digit4': case 'Numpad4':
                case 'Digit5': case 'Numpad5':
                case 'Digit6': case 'Numpad6':
                case 'Digit7': case 'Numpad7':
                case 'Digit8': case 'Numpad8':
                case 'Digit9': case 'Numpad9':
                    pressDigit(parseInt(str.substr(str.length - 1)));
                    break;

                case 'Ctrl+Digit1': case 'Ctrl+Numpad1':
                case 'Ctrl+Digit2': case 'Ctrl+Numpad2':
                case 'Ctrl+Digit3': case 'Ctrl+Numpad3':
                case 'Ctrl+Digit4': case 'Ctrl+Numpad4':
                case 'Ctrl+Digit5': case 'Ctrl+Numpad5':
                case 'Ctrl+Digit6': case 'Ctrl+Numpad6':
                case 'Ctrl+Digit7': case 'Ctrl+Numpad7':
                case 'Ctrl+Digit8': case 'Ctrl+Numpad8':
                case 'Ctrl+Digit9': case 'Ctrl+Numpad9':
                    enterCenterNotation(parseInt(str.substr(str.length - 1)));
                    break;

                case 'Ctrl+Shift+Digit1': case 'Ctrl+Shift+Numpad1':
                case 'Ctrl+Shift+Digit2': case 'Ctrl+Shift+Numpad2':
                case 'Ctrl+Shift+Digit3': case 'Ctrl+Shift+Numpad3':
                case 'Ctrl+Shift+Digit4': case 'Ctrl+Shift+Numpad4':
                case 'Ctrl+Shift+Digit5': case 'Ctrl+Shift+Numpad5':
                case 'Ctrl+Shift+Digit6': case 'Ctrl+Shift+Numpad6':
                case 'Ctrl+Shift+Digit7': case 'Ctrl+Shift+Numpad7':
                case 'Ctrl+Shift+Digit8': case 'Ctrl+Shift+Numpad8':
                case 'Ctrl+Shift+Digit9': case 'Ctrl+Shift+Numpad9':
                    setCellColor(parseInt(str.substr(str.length - 1)) - 1);
                    break;

                case 'Shift+Digit1': case 'Shift+Numpad1':
                case 'Shift+Digit2': case 'Shift+Numpad2':
                case 'Shift+Digit3': case 'Shift+Numpad3':
                case 'Shift+Digit4': case 'Shift+Numpad4':
                case 'Shift+Digit5': case 'Shift+Numpad5':
                case 'Shift+Digit6': case 'Shift+Numpad6':
                case 'Shift+Digit7': case 'Shift+Numpad7':
                case 'Shift+Digit8': case 'Shift+Numpad8':
                case 'Shift+Digit9': case 'Shift+Numpad9':
                    let digit = parseInt(str.substr(str.length - 1));
                    if (selectedCells.length > 0)
                        enterCornerNotation(digit);
                    else
                    {
                        if (highlightedDigits.includes(digit))
                            highlightedDigits.splice(highlightedDigits.indexOf(digit), 1);
                        else
                            highlightedDigits.push(digit);
                        updateVisuals();
                    }
                    break;

                case 'Delete': clearCells(); break;

                case 'Ctrl+KeyC':
                case 'Ctrl+Insert':
                    navigator.clipboard.writeText(selectedCells.map(c => state.enteredDigits[c] || '.').join(''));
                    break;

                // Navigation
                case 'KeyZ': mode = 'normal'; updateVisuals(); break;
                case 'KeyX': mode = 'corner'; updateVisuals(); break;
                case 'KeyC': mode = 'center'; updateVisuals(); break;
                case 'KeyV': mode = 'color'; updateVisuals(); break;
                case 'Space': {
                    let modes = ['normal', 'corner', 'center', 'color'];
                    mode = modes[(modes.indexOf(mode) + 1) % modes.length];
                    updateVisuals();
                    break;
                }

                case 'ArrowUp': ArrowMovement(0, -1, 'clear'); break;
                case 'ArrowDown': ArrowMovement(0, 1, 'clear'); break;
                case 'ArrowLeft': ArrowMovement(-1, 0, 'clear'); break;
                case 'ArrowRight': ArrowMovement(1, 0, 'clear'); break;
                case 'Shift+ArrowUp': ArrowMovement(0, -1, 'add'); break;
                case 'Shift+ArrowDown': ArrowMovement(0, 1, 'add'); break;
                case 'Shift+ArrowLeft': ArrowMovement(-1, 0, 'add'); break;
                case 'Shift+ArrowRight': ArrowMovement(1, 0, 'add'); break;
                case 'Ctrl+ArrowUp': ArrowMovement(0, -1, 'move'); break;
                case 'Ctrl+ArrowDown': ArrowMovement(0, 1, 'move'); break;
                case 'Ctrl+ArrowLeft': ArrowMovement(-1, 0, 'move'); break;
                case 'Ctrl+ArrowRight': ArrowMovement(1, 0, 'move'); break;
                case 'Ctrl+ControlLeft': case 'Ctrl+ControlRight': keepMove = true; break;
                case 'Ctrl+Space':
                    if (highlightedDigits.length > 0)
                    {
                        selectedCells = [];
                        for (let cell = 0; cell < 81; cell++)
                            if (highlightedDigits.includes(getDisplayedSudokuDigit(state, cell)))
                                selectedCells.push(cell);
                        highlightedDigits = [];
                    }
                    else if (selectedCells.length >= 2 && selectedCells[selectedCells.length - 2] === selectedCells[selectedCells.length - 1])
                        selectedCells.splice(selectedCells.length - 1, 1);
                    else
                        keepMove = !keepMove;
                    updateVisuals();
                    break;
                case 'Escape': selectedCells = []; highlightedDigits = []; updateVisuals(); break;
                case 'Ctrl+KeyA': selectedCells = Array(81).fill(null).map((_, c) => c); updateVisuals(); break;

                // Undo/redo
                case 'Backspace':
                case 'Ctrl+KeyZ':
                    undo();
                    break;

                case 'Shift+Backspace':
                case 'Ctrl+KeyY':
                    redo();
                    break;

                // Debug
                case 'KeyL':
                    console.log(selectedCells.join(", "));
                    break;

                default:
                    anyFunction = false;
                    console.log(str, ev.code);
                    break;
            }

            if (anyFunction)
            {
                ev.stopPropagation();
                ev.preventDefault();
                return false;
            }
        });

        puzzleDiv.onmousedown = function(ev)
        {
            if (!ev.shiftKey && !ev.ctrlKey)
            {
                selectedCells = [];
                highlightedDigits = [];
                updateVisuals();
                remoteLog2(`onmousedown puzzleDiv`);
            }
            else
                remoteLog2(`onmousedown puzzleDiv (canceled)`);
        };

        let puzzleSvg = puzzleDiv.getElementsByTagName('svg')[0];

        // Step 1: move the button row so that it’s below the puzzle
        let buttonRow = document.getElementById('button-row');
        let extraBBox = document.getElementById('sudoku').getBBox();
        buttonRow.setAttribute('transform', `translate(0, ${Math.max(9, extraBBox.y + extraBBox.height) + .25})`);

        // Step 2: change the viewBox so that it includes everything
        let fullBBox = document.getElementById('full-puzzle').getBBox();
        puzzleSvg.setAttribute('viewBox', `${fullBBox.x - .1} ${fullBBox.y - .1} ${fullBBox.width + .2} ${fullBBox.height + .5}`);
    });
});
