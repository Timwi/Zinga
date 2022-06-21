window.onload = (function()
{
    /// — FUNCTIONS

    // Utility
    function dotNet(method, args, callback)
    {
        if (blazorQueue === null)
        {
            if (method === null)
            {
                if (callback)
                    callback();
            }
            else
            {
                let start = new Date();
                DotNet.invokeMethodAsync('ZingaWasm', method, ...args).then(result =>
                {
                    if (outputBlazorTimings)
                        console.log(`${method} took ${new Date() - start} ms.`);
                    if (callback)
                        callback(result);
                });
            }
        }
        else
            blazorQueue.push([method, args, callback]);
    }
    function fixViewBox()
    {
        // Fix the viewBox
        let puzzleSvg = puzzleDiv.querySelector('svg.puzzle-svg');

        // Step 1: move the button row so that it’s below the puzzle
        let buttons = document.getElementById('bb-buttons');
        let extraBBox = document.getElementById('bb-puzzle-without-global').getBBox({ fill: true, stroke: true, markers: true, clipped: true });
        buttons.setAttribute('transform', `translate(0, ${Math.max(9.4, extraBBox.y + extraBBox.height + .25)})`);

        // Step 2: move the global constraints so they’re to the left of the puzzle
        let globalBox = document.getElementById('constraint-svg-global');
        globalBox.setAttribute('transform', `translate(${extraBBox.x - 1.5}, 0)`);

        // Step 3: change the viewBox so that it includes everything
        let fullBBox = document.getElementById('bb-everything').getBBox({ fill: true, stroke: true, markers: true, clipped: true });
        let left = Math.min(-.4, fullBBox.x - .1);
        let top = Math.min(-.4, fullBBox.y - .1);
        let right = Math.max(9.4, fullBBox.x + fullBBox.width + .2);
        let bottom = Math.max(9.4, fullBBox.y + fullBBox.height + .2);
        puzzleSvg.setAttribute('viewBox', `${left} ${top} ${right - left} ${bottom - top}`);
        let selectionFilter = document.getElementById('constraint-invalid-shadow');
        selectionFilter.setAttribute('x', left);
        selectionFilter.setAttribute('y', top);
        selectionFilter.setAttribute('width', right - left);
        selectionFilter.setAttribute('height', bottom - top);
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
    function setButtonHandler(btn, click)
    {
        btn.onclick = handler(ev => click(ev));
        btn.onmousedown = handler(function() { });
    }
    function setClass(elem, className, setUnset)
    {
        if (setUnset)
            elem.classList.add(className);
        else
            elem.classList.remove(className);
    }

    // Puzzle
    function checkSudokuValid()
    {
        // Checks only the basic Sudoku rules, not the constraints

        let grid = Array(81).fill(null).map((_, c) => getDisplayedSudokuDigit(state, c));
        Array.from(document.querySelectorAll('.region-invalid')).forEach(elem => { elem.setAttribute('opacity', 0); });
        let isValid = true;

        // Check the Sudoku rules (rows, columns and boxes)
        for (let i = 0; i < 9; i++)
        {
            // Check rows
            let go = true;
            for (let colA = 0; colA < 9 && go; colA++)
                for (let colB = colA + 1; colB < 9 && go; colB++)
                    if (grid[colA + 9 * i] !== null && grid[colA + 9 * i] === grid[colB + 9 * i])
                    {
                        if (showErrors)
                            document.getElementById(`row-invalid-${i}`).setAttribute('opacity', 1);
                        isValid = false;
                        go = false;
                    }
            // Check columns
            go = true;
            for (let rowA = 0; rowA < 9 && go; rowA++)
                for (let rowB = rowA + 1; rowB < 9 && go; rowB++)
                    if (grid[i + 9 * rowA] !== null && grid[i + 9 * rowA] === grid[i + 9 * rowB])
                    {
                        if (showErrors)
                            document.getElementById(`column-invalid-${i}`).setAttribute('opacity', 1);
                        isValid = false;
                        go = false;
                    }
            // Check boxes
            go = true;
            for (let cellA = 0; cellA < 9 && go; cellA++)
                for (let cellB = cellA + 1; cellB < 9 && go; cellB++)
                    if (grid[cellA % 3 + 3 * (i % 3) + 9 * (((cellA / 3) | 0) + 3 * ((i / 3) | 0))] !== null &&
                        grid[cellA % 3 + 3 * (i % 3) + 9 * (((cellA / 3) | 0) + 3 * ((i / 3) | 0))] === grid[cellB % 3 + 3 * (i % 3) + 9 * (((cellB / 3) | 0) + 3 * ((i / 3) | 0))])
                    {
                        if (showErrors)
                            document.getElementById(`box-invalid-${i}`).setAttribute('opacity', 1);
                        isValid = false;
                        go = false;
                    }
        }

        // Check that all cells in the Sudoku grid have a digit
        if (isValid && grid.some(c => c === null))
            isValid = null;

        setClass(puzzleDiv, 'solved', false);

        // Check if any constraints are violated
        if (showErrors || grid.every(d => d !== null))
        {
            dotNet('CheckConstraints', [JSON.stringify(grid), JSON.stringify(constraints)], result =>
            {
                let violatedConstraintIxs = JSON.parse(result);
                setClass(puzzleDiv, 'solved', isValid === true && violatedConstraintIxs.length === 0);
                for (let cIx = 0; cIx < constraints.length; cIx++)
                {
                    if (violatedConstraintIxs.includes(cIx) && showErrors)
                    {
                        console.log(`Constraint ${cIx} is violated.`);
                        document.getElementById(`constraint-svg-${cIx}`).classList.add('violated');
                    }
                    else
                        document.getElementById(`constraint-svg-${cIx}`).classList.remove('violated');
                }
            });
        }
        else
            Array.from(document.querySelectorAll('#constraint-svg>g,#constraint-svg-global>g')).forEach(g => { g.removeAttribute('filter'); });
    }
    function clearCells()
    {
        if (mode === 'color' && selectedCells.some(c => state.colors[c].length > 0))
        {
            saveUndo();
            for (let cell of selectedCells)
                state.colors[cell] = [];
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
            let colorCode = val % 3n;
            val = val / 3n;
            switch (Number(colorCode))
            {
                case 1: // single color
                    st.colors[cell] = [Number(val % 9n)];
                    val = val / 9n;
                    break;

                case 2: // multi-color
                    st.colors[cell] = [];
                    for (let color = 9 - 1; color >= 0; color--)
                    {
                        if (val % 2n != 0n)
                            st.colors[cell].push(color);
                        val = val / 2n;
                    }
                    st.colors[cell].sort();
                    break;

                default:    // no color
                    st.colors[cell] = [];
                    break;
            }

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

            // Encode colors
            if (st.colors[cell].length === 0)   // Common case: no color
                val = (val * 3n);
            else if (st.colors[cell].length === 1)  // Single color: one of 9
                val = (val * 9n + BigInt(st.colors[cell][0])) * 3n + 1n;
            else    // Multiple colors: bitfield
            {
                for (let color = 0; color < 9; color++)
                    val = (val * 2n) + (st.colors[cell].includes(color) ? 1n : 0n);
                val = val * 3n + 2n;
            }
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
    function getDisplayedSudokuDigit(st, cell)
    {
        return givens[cell] !== null ? givens[cell] : st.enteredDigits[cell];
    }
    function makeCleanState()
    {
        return {
            colors: Array(81).fill(null).map(_ => []),
            cornerNotation: Array(81).fill(null).map(_ => []),
            centerNotation: Array(81).fill(null).map(_ => []),
            enteredDigits: Array(81).fill(null)
        };
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
    function setCellColor(color)
    {
        if (selectedCells.length === 0)
            return;
        saveUndo();
        if (selectedCells.every(cell => state.colors[cell].includes(color)))
        {
            for (let cell of selectedCells)
                state.colors[cell].splice(state.colors[cell].indexOf(color), 1);
        }
        else
        {
            for (let cell of selectedCells)
                if (multiColorMode)
                {
                    if (!state.colors[cell].includes(color))
                    {
                        state.colors[cell].push(color);
                        state.colors[cell].sort();
                    }
                }
                else
                    state.colors[cell] = [color];
        }
        updateVisuals(true);
    }

    // Undo/redo
    function undo()
    {
        if (undoBuffer.length > 0)
        {
            redoBuffer.push(encodeState(state));
            state = decodeState(undoBuffer.pop());
            updateVisuals(true);
        }
    }
    function redo()
    {
        if (redoBuffer.length > 0)
        {
            undoBuffer.push(encodeState(state));
            state = decodeState(redoBuffer.pop());
            updateVisuals(true);
        }
    }
    function saveUndo()
    {
        undoBuffer.push(encodeState(state));
        redoBuffer = [];
    }

    // UI
    function resetClearButton()
    {
        document.getElementById(`btn-clear`).classList.remove('warning');
        document.querySelector(`#btn-clear>text`).textContent = 'Clear';
    }
    function updateConstraints()
    {
        // This function is only used on the test page

        if (!document.hidden && puzzleId === 'test')
        {
            if (outputBlazorTimings)
                console.log('Re-initializing');
            let state = null;
            try { state = JSON.parse(localStorage.getItem(`zinga-edit`)); }
            catch { }
            if (state && state.givens && state.constraints)
            {
                givens = state.givens;
                constraints = state.constraints;
                customConstraintTypes = state.customConstraintTypes;

                document.querySelector('#topbar>.title').innerText = state.title ?? 'Sudoku';
                document.querySelector('#topbar>.author').innerText = `by ${state.author ?? 'unknown'}`;
                document.querySelector('#sidebar .links').innerHTML = Array.isArray(state.links) ? state.links.map(_ => `<li><a></a></li>`).join('') : '';
                Array.from(document.querySelectorAll('#sidebar .links a')).forEach((obj, ix) => { obj.setAttribute('href', state.links[ix].url); obj.innerText = state.links[ix].text; });
                document.title = `Testing: ${state.title ?? 'Sudoku'} by ${state.author ?? 'unknown'}`;

                puzzleDiv.dataset.title = state.title;
                puzzleDiv.dataset.author = state.author;

                var paragraphs = (state.rules ?? 'Normal Sudoku rules apply: place the digits 1–9 in every row, every column and every 3×3 box.').split(/\r?\n/).filter(s => s !== null && !/^\s*$/.test(s));
                document.getElementById('rules-text').innerHTML = paragraphs.map(_ => '<p></p>').join('');
                Array.from(document.querySelectorAll('#rules-text>p')).forEach((p, pIx) => { p.innerText = paragraphs[pIx]; });

                dotNet('SetupConstraints', [JSON.stringify(givens), JSON.stringify(constraintTypes), JSON.stringify(state.customConstraintTypes), JSON.stringify(state.constraints)]);

                dotNet('RenderConstraintSvgs', [JSON.stringify(constraintTypes), JSON.stringify(customConstraintTypes), JSON.stringify(constraints)], resultsRaw =>
                {
                    let results = JSON.parse(resultsRaw);

                    let globalSvgs = '', svgs = '', globalY = 0;
                    for (let cIx = 0; cIx < results.svgs.length; cIx++)
                        if (results.svgs[cIx].global)
                        {
                            globalSvgs += `<g id='constraint-svg-${cIx}' transform='translate(0, ${globalY})'>${results.svgs[cIx].svg}</g>`;
                            globalY += 1.5;
                        }
                        else
                            svgs += `<g class='constraint-svg' id='constraint-svg-${cIx}'>${results.svgs[cIx].svg}</g>`;

                    document.getElementById('constraint-svg-global').innerHTML = globalSvgs;
                    document.getElementById('constraint-svg').innerHTML = svgs;
                    document.getElementById('constraint-defs').innerHTML = results.svgDefs;
                    updateVisuals();
                    fixViewBox();
                    window.setTimeout(function() { window.dispatchEvent(new Event('resize')); }, 10);
                });
            }
        }
    }
    function updateVisuals(udpateStorage)
    {
        // Update localStorage (only do this when necessary because encodeState() is relatively slow on Firefox)
        if (localStorage && udpateStorage)
        {
            localStorage.setItem(`zinga-${puzzleId}`, encodeState(state));
            localStorage.setItem(`zinga-${puzzleId}-undo`, undoBuffer.join(' '));
            localStorage.setItem(`zinga-${puzzleId}-redo`, redoBuffer.join(' '));
            localStorage.setItem(`zinga-${puzzleId}-opt`, JSON.stringify({ showErrors: showErrors, multiColorMode: multiColorMode, sidebarOn: sidebarOn }));
        }
        resetClearButton();

        // Sudoku grid (digits, selection/highlight)
        let digitCounts = Array(9).fill(0);
        Array.from(document.querySelectorAll('.cell')).forEach(cellElem =>
        {
            let cell = cellElem.dataset.cell | 0;
            setClass(cellElem, 'highlighted', selectedCells.includes(cell) || highlightedDigits.includes(getDisplayedSudokuDigit(state, cell)));
            for (let color = 0; color < 9; color++)
                setClass(cellElem, `c${color}`, state.colors[cell].length >= 1 && state.colors[cell][0] === color);
        });
        for (let cell = 0; cell < 81; cell++)
        {
            let digit = getDisplayedSudokuDigit(state, cell);
            digitCounts[digit - 1]++;

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

            function getPerimeterPoint(angle)
            {
                function tan(θ) { return Math.tan(θ * Math.PI / 180); }
                if (angle > -45 && angle <= 45)
                    return ` .5 ${.5 * tan(angle)}`;
                if (angle > 45 && angle <= 135)
                    return ` ${-.5 * tan(angle - 90)} .5`;
                if (angle > 135 && angle <= 225)
                    return ` -.5 ${-.5 * tan(angle - 180)}`;
                return ` ${.5 * tan(angle - 270)} -.5`;
            }
            let multiColorSvg = '';
            for (let i = 1; i < state.colors[cell].length; i++)
            {
                let angle1 = -70 + 360 * i / state.colors[cell].length;
                let angle2 = i === state.colors[cell].length - 1 ? 290 : 270;
                let path = 'M 0 0' + getPerimeterPoint(angle1);
                if (angle1 < -45 && angle2 > -45)
                    path += ' .5 -.5';
                if (angle1 < 45 && angle2 > 45)
                    path += ' .5 .5';
                if (angle1 < 135 && angle2 > 135)
                    path += ' -.5 .5';
                if (angle1 < 225 && angle2 > 225)
                    path += ' -.5 -.5';
                path += getPerimeterPoint(angle2);
                multiColorSvg += `<path d='${path}z' class='c${state.colors[cell][i]}' />`;
            }
            document.getElementById(`sudoku-multicolor-${cell}`).innerHTML = multiColorSvg;
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

        setClass(puzzleDiv, 'sidebar-off', !sidebarOn);
        puzzleDiv.querySelector('#btn-sidebar>text').textContent = sidebarOn ? 'Less' : 'More';
        puzzleDiv.querySelector('#opt-show-errors').checked = showErrors;
        puzzleDiv.querySelector('#opt-multi-color').checked = multiColorMode;

        // Check if there are any conflicts (red glow) and/or the puzzle is solved
        checkSudokuValid();
    }

    // Debugging
    function remoteLog(msg)
    {
        //let req = new XMLHttpRequest();
        //req.open('POST', '/remote-log', true);
        //req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        //req.send(`msg=${encodeURIComponent(msg)}`);
    }
    function remoteLog2(msg)
    {
        remoteLog(`${msg} [${selectedCells.join()}] ${draggingMode ?? "null"}`);
    }




    /// — INITIALIZATION

    // Blazor
    let outputBlazorTimings = false;
    let blazorQueue = [];

    // Variables: UI elements
    let puzzleDiv = document.getElementById('puzzle');
    let puzzleContainer = document.getElementById('puzzle-container');

    // Variables: UI other
    let draggingMode = null;
    let highlightedDigits = [];
    let keepMove = false;
    let mode = 'normal';
    let multiColorMode = false;
    let selectedCells = [];
    let showErrors = true;
    let sidebarOn = true;

    // Variables: puzzle
    let puzzleId = puzzleDiv.dataset.puzzleid || 'unknown';
    let constraintTypes = JSON.parse(puzzleDiv.dataset.constrainttypes);
    let customConstraintTypes = [];
    let constraints = puzzleId === 'test' ? [] : JSON.parse(puzzleDiv.dataset.constraints);
    let givens = Array(81).fill(null);
    let state = makeCleanState();
    let undoBuffer = [];
    let redoBuffer = [];


    // Events
    puzzleContainer.onmouseup = handler(puzzleContainer.ontouchend = function(ev)
    {
        if (ev.type !== 'touchend' || ev.touches.length === 0)
            draggingMode = null;
        remoteLog(`${ev.type} puzzleContainer`);
    });
    Array.from(puzzleDiv.getElementsByClassName('sudoku-cell')).forEach(cellRect =>
    {
        let cell = parseInt(cellRect.dataset.cell);
        cellRect.onclick = handler(function() { remoteLog2(`onclick ${cell}`); });
        cellRect.onmousedown = cellRect.ontouchstart = handler(function(ev)
        {
            puzzleContainer.focus();
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
    Array(9).fill(null).forEach((_, btn) =>
    {
        setButtonHandler(document.getElementById(`btn-${btn + 1}`), function(ev) { pressDigit(btn + 1, ev); });
    });
    ["normal", "corner", "center", "color"].forEach(btn => setButtonHandler(puzzleDiv.querySelector(`#btn-${btn}>rect`), function()
    {
        mode = btn;
        updateVisuals();
    }));
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
            state = makeCleanState();
            updateVisuals(true);
        }
    });
    setButtonHandler(puzzleDiv.querySelector(`#btn-undo>rect`), undo);
    setButtonHandler(puzzleDiv.querySelector(`#btn-redo>rect`), redo);
    setButtonHandler(puzzleDiv.querySelector(`#btn-sidebar>rect`), function() { sidebarOn = !sidebarOn; updateVisuals(true); });
    document.getElementById(`opt-show-errors`).onchange = function() { showErrors = !showErrors; updateVisuals(true); };
    document.getElementById(`opt-multi-color`).onchange = function() { multiColorMode = !multiColorMode; updateVisuals(true); };
    setButtonHandler(document.getElementById('opt-edit'), () =>
    {
        if (puzzleId !== 'test')
        {
            let editUndoBuffer = [], editState = null;

            let undoB = localStorage.getItem('zinga-edit-undo');
            try { editUndoBuffer = undoB ? JSON.parse(undoB) : []; }
            catch { }

            let str = localStorage.getItem('zinga-edit');
            try { editState = JSON.parse(str); }
            catch { }

            if (editState !== null)
                editUndoBuffer.push(editState);

            localStorage.setItem('zinga-edit-undo', JSON.stringify(editUndoBuffer));
            localStorage.setItem('zinga-edit-redo', "[]");

            let newEditState = {
                title: puzzleDiv.dataset.title,
                author: puzzleDiv.dataset.author,
                rules: puzzleDiv.dataset.rules,
                givens: givens,
                constraints: [],
                customConstraintTypes: []
            };

            for (let c of Object.keys(constraintTypes))
                if (!constraintTypes[c].public)
                    newEditState.customConstraintTypes.push(constraintTypes[c]);
            for (let c of constraints)
                newEditState.constraints.push({ type: constraintTypes[c.type].public ? c.type : ~newEditState.customConstraintTypes.indexOf(constraintTypes[c.type]), values: c.values });

            localStorage.setItem('zinga-edit', JSON.stringify(newEditState));
        }
        window.open(`${window.location.protocol}//${window.location.host}/edit`);
    });
    setButtonHandler(document.getElementById('opt-screenshot'), () =>
    {
        let img = new Image();
        let svgElem = puzzleDiv.querySelector('svg.puzzle-svg');
        let bBox = document.getElementById('bb-puzzle-with-global').getBBox({ fill: true, stroke: true, markers: true });
        let margin = .35;
        let nBox = { x: bBox.x - margin, y: bBox.y - margin, width: bBox.width + 2 * margin, height: bBox.height + 2 * margin };
        let canvas = document.createElement('canvas');
        canvas.width = 1000;
        canvas.height = canvas.width / nBox.width * nBox.height;
        img.onload = function()
        {
            canvas.getContext('2d').drawImage(img, 0, 0);
            if (navigator.userAgent.includes('Gecko/'))
                window.open(canvas.toDataURL());
            else
            {
                let a = document.createElement('a');
                a.setAttribute('href', canvas.toDataURL());
                a.setAttribute('download', `${puzzleDiv.dataset.title} by ${puzzleDiv.dataset.author}.png`);
                a.click();
            }
        };
        img.onerror = function()
        {
            alert('Unfortunately, a screenshot of the puzzle could not be generated. This may be due to malformed SVG code on the part of the puzzle author.');
        };
        img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgElem.outerHTML
            .replace(/<svg/, `<svg width="${canvas.width}" height="${canvas.height}"`)
            .replace(/viewBox=".*?"/, `viewBox='${nBox.x} ${nBox.y} ${nBox.width} ${nBox.height}'`)
            .replace(/(?=<\/style>)/, Array.from(document.styleSheets).reduce((p, ss) => p.concat(Array.from(ss.rules)), [])
                .filter(rule => !(rule instanceof CSSStyleRule) || rule.selectorText.startsWith('svg.puzzle-svg ')).map(rule => rule.cssText.replace(/^\s*svg\.puzzle-svg\s/, '')).join("\n")))));
    });
    puzzleContainer.addEventListener("keydown", ev =>
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
                navigator.clipboard.writeText(selectedCells.map(c => getDisplayedSudokuDigit(state, c) || '.').join(''));
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
            case 'Shift+Space': {
                let modes = ['normal', 'corner', 'center', 'color'];
                mode = modes[(modes.indexOf(mode) + modes.length - 1) % modes.length];
                updateVisuals();
                break;
            }

            case 'Slash':
                sidebarOn = !sidebarOn;
                updateVisuals(true);
                break;

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
            case 'Ctrl+KeyO':
                console.log(selectedCells.join(", "));
                break;

            default:
                anyFunction = false;
                //console.log(str, ev.code);
                break;
        }

        if (anyFunction)
        {
            ev.stopPropagation();
            ev.preventDefault();
            return false;
        }
    });
    puzzleContainer.onmousedown = function(ev)
    {
        if (!ev.shiftKey && !ev.ctrlKey)
        {
            selectedCells = [];
            highlightedDigits = [];
            updateVisuals();
            remoteLog2(`onmousedown puzzleContainer`);
        }
        else
            remoteLog2(`onmousedown puzzleContainer (canceled)`);
    };
    window.addEventListener('resize', function()
    {
        let rulesDiv = document.getElementById('rules-text');
        let sidebar = document.getElementById('sidebar');
        let sidebarContent = document.getElementById('sidebar-content');
        let min = 8;
        let max = 18;

        while (max - min > .1)
        {
            let mid = (max + min) / 2;
            rulesDiv.style.fontSize = `${mid}pt`;
            if (sidebarContent.scrollHeight > sidebar.offsetHeight)
                max = mid;
            else
                min = mid;
        }
        rulesDiv.style.fontSize = `${min}pt`;
    });
    window.setTimeout(function() { window.dispatchEvent(new Event('resize')); }, 10);


    /// — RUN

    // Blazor
    Blazor.start({}).then(() =>
    {
        for (let i = 0; i < blazorQueue.length; i++)
        {
            let bq = blazorQueue[i];
            if (bq[0] === null)
                bq[2]();
            else
            {
                let start = new Date();
                var r = DotNet.invokeMethodAsync('ZingaWasm', bq[0], ...bq[1]).then(result =>
                {
                    if (outputBlazorTimings)
                        console.log(`${bq[0]} took ${new Date() - start} ms.`);
                    if (bq[2])
                        r.then(bq[2](result));
                });
            }
        }
        blazorQueue = null;
    });

    // Retrieve state from localStorage
    try
    {
        let optB = localStorage.getItem(`zinga-${puzzleId}-opt`);
        let opt = optB && JSON.parse(optB);

        showErrors = opt ? !!opt.showErrors : true;
        multiColorMode = opt ? !!opt.multiColorMode : false;
        sidebarOn = opt ? !!opt.sidebarOn : true;

        let str = localStorage.getItem(`zinga-${puzzleId}`);
        let item = null;
        if (str !== null)
            try { item = decodeState(str); }
            catch { item = null; }
        if (item && item.cornerNotation && item.centerNotation && item.enteredDigits && item.colors)
            state = item;

        let undoB = localStorage.getItem(`zinga-${puzzleId}-undo`);
        let redoB = localStorage.getItem(`zinga-${puzzleId}-redo`);

        undoBuffer = undoB ? undoB.split(' ') : [];
        redoBuffer = redoB ? redoB.split(' ') : [];
    }
    catch { }

    // Puzzle
    if (puzzleId !== 'test')
        for (let givenInf of JSON.parse(puzzleDiv.dataset.givens ?? null) ?? [])
            givens[givenInf[0]] = givenInf[1];

    // UI
    if (puzzleId === 'test')
    {
        updateConstraints();    // Make sure this happens before the first call to updateVisuals()
        document.addEventListener('visibilitychange', updateConstraints);
    }
    else
    {
        dotNet('SetupConstraints', [JSON.stringify(givens), JSON.stringify(constraintTypes), JSON.stringify(customConstraintTypes), JSON.stringify(constraints)]);
    }

    updateVisuals(true);
    puzzleContainer.focus();
    fixViewBox();
});
