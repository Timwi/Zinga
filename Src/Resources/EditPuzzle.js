window.onload = (function()
{
    function remoteLog(msg)
    {
        //let req = new XMLHttpRequest();
        //req.open('POST', '/remote-log', true);
        //req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        //req.send(`msg=${encodeURIComponent(msg)}`);
    }

    function handler(fnc)
    {
        return function(ev)
        {
            if (fnc(ev) !== true)
            {
                ev.stopPropagation();
                ev.preventDefault();
                return false;
            }
        };
    }

    function setClass(elem, className, setUnset)
    {
        if (setUnset)
            elem.classList.add(className);
        else
            elem.classList.remove(className);
    }

    let blazorQueue = [];
    function dotNet(method, args, callback)
    {
        if (blazorQueue === null)
            DotNet.invokeMethodAsync('ZingaWasm', method, ...args).then(callback);
        else
            blazorQueue.push([method, args, callback]);
    }
    function afterBlazor(fnc)
    {
        if (blazorQueue === null)
            fnc();
        else
            blazorQueue.push([null, null, fnc]);
    }
    Blazor.start({})
        .then(() =>
        {
            for (let i = 0; i < blazorQueue.length; i++)
            {
                if (blazorQueue[i][0] === null)
                    blazorQueue[i][2]();
                else
                    DotNet.invokeMethodAsync('ZingaWasm', blazorQueue[i][0], ...blazorQueue[i][1]).then(blazorQueue[i][2]);
            }
            blazorQueue = null;
        });

    let puzzleDiv = document.querySelector('div.puzzle');
    let puzzleContainer = puzzleDiv.querySelector('.puzzle-container');
    let sidebarDiv = document.querySelector('div.sidebar');
    let puzzleId = puzzleDiv.dataset.puzzleid || 'unknown';
    let constraintTypes = JSON.parse(puzzleDiv.dataset.constrainttypes || null) || [];

    let draggingMode = null;
    puzzleContainer.onmouseup = handler(puzzleContainer.ontouchend = function(ev)
    {
        if (ev.type !== 'touchend' || ev.touches.length === 0)
            draggingMode = null;
        remoteLog(`${ev.type} puzzleContainer`);
    });

    function encodeState(st)
    {
        return JSON.stringify(st);
    }

    function decodeState(str)
    {
        return JSON.parse(str);
    }

    let state = {
        givens: Array(81).fill(null),
        constraints: []
    };
    if (puzzleDiv.dataset.constraints)
        state.constraints = JSON.parse(puzzleDiv.dataset.constraints);
    for (let givenInf of JSON.parse(puzzleDiv.dataset.givens || null) || [])
        state.givens[givenInf[0]] = givenInf[1];

    let undoBuffer = [encodeState(state)];
    let redoBuffer = [];

    let selectedCells = [];
    let selectedConstraints = [];
    let lastSelectedConstraint = 0;

    function remoteLog2(msg)
    {
        remoteLog(`${msg} [${selectedCells.join()}] ${draggingMode ?? "null"}`);
    }

    try
    {
        //let undoB = localStorage.getItem(`zinga-edit-${puzzleId}-undo`);
        //let redoB = localStorage.getItem(`zinga-edit-${puzzleId}-redo`);

        //undoBuffer = undoB ? JSON.parse(undoB) : [encodeState(state)];
        //redoBuffer = redoB ? JSON.parse(redoB) : [];

        //str = localStorage.getItem(`zinga-edit-${puzzleId}`);
        //if (str !== null)
        //    try { item = JSON.parse(str); }
        //    catch { item = decodeState(str); }
        //if (item && item.givens && item.constraints)
        //    state = item;
    }
    catch
    {
    }

    function resetClearButton()
    {
        document.getElementById(`btn-clear`).classList.remove('warning');
        document.querySelector(`#btn-clear>text`).textContent = 'Delete';
    }

    function selectConstraint(cIx)
    {
        selectedConstraints = [cIx];
        lastSelectedConstraint = cIx;
        selectedCells = [];
        updateVisuals();
    }

    function selectConstraintRange(cIx1, cIx2)
    {
        if (cIx1 <= cIx2)
            selectedConstraints = Array(cIx2 - cIx1 + 1).fill(null).map((_, c) => c + cIx1);
        else
            selectedConstraints = Array(cIx1 - cIx2 + 1).fill(null).map((_, c) => c + cIx2);
        selectedCells = [];
        updateVisuals();
    }

    // options:
    //  storage (bool)
    //  svg (bool)          — updates constraint SVG in the grid (this involves Blazor)
    //  ui (bool)            — updates constraint UI in the sidebar
    function updateVisuals(opt)
    {
        // Update localStorage (only do this when necessary because encodeState() is relatively slow on Firefox)
        if (localStorage && opt && opt.storage)
        {
            localStorage.setItem(`zinga-edit-${puzzleId}`, encodeState(state));
            localStorage.setItem(`zinga-edit-${puzzleId}-undo`, JSON.stringify(undoBuffer));
            localStorage.setItem(`zinga-edit-${puzzleId}-redo`, JSON.stringify(redoBuffer));
        }
        resetClearButton();

        function updateConstraintSelection()
        {
            for (let cIx = 0; cIx < state.constraints.length; cIx++)
            {
                if (selectedConstraints.includes(cIx))
                    document.getElementById(`constraint-svg-${cIx}`).setAttribute('filter', 'url(#constraint-selection-shadow)');
                else
                    document.getElementById(`constraint-svg-${cIx}`).removeAttribute('filter');
            }
        }

        let constraintSelectionUpdated = false;
        if (opt && opt.svg)
        {
            // Constraint SVGs
            constraintSelectionUpdated = true;
            dotNet('RenderConstraintSvgs', [JSON.stringify(constraintTypes), JSON.stringify(state.constraints)], svgs =>
            {
                let list = JSON.parse(svgs);
                document.getElementById('constraint-defs').innerHTML = list[0];
                document.getElementById('constraint-svg').innerHTML = list[1];
                updateConstraintSelection();
            });
        }

        if (opt && opt.ui)
        {
            function variableUi(type, value, id)
            {
                switch (type)
                {
                    case 'int':
                        return `<input type='number' step='1' id='${id}' value='${value}' />`;
                    case 'bool':
                        return `<input type='checkbox' id='${id}'${value ? ` checked='checked'` : ''} />`;
                }

                let listRx = /^list\((.*)\)$/.exec(type);
                if (listRx !== null)
                {
                    var uis = [];
                    for (let i = 0; i < value.length; i++)
                        uis.push(`<li>${variableUi(listRx[1], value[i], `${id}-${i}`)}</li>`);
                    return `<ul>${uis.join('')}</ul>`;
                }

                return "(not implemented)";
            }

            function setVariableEvents(type, setter, id)
            {
                switch (type)
                {
                    case 'int':
                        document.getElementById(id).onchange = function() { setter(document.getElementById(id).value); };
                        return;
                    case 'bool':
                        document.getElementById(id).onchange = function() { setter(document.getElementById(id).checked); };
                        return;
                }
            }

            // List of constraints
            let constraintListHtml = '';
            for (var cIx = 0; cIx < state.constraints.length; cIx++)
            {
                let constraint = state.constraints[cIx];
                let variableHtml = '';
                for (let v of Object.keys(constraintTypes[constraint.type].variables))
                    variableHtml += `<div class='variable'><div class='name'>${v}</div><div class='value'>${variableUi(constraintTypes[constraint.type].variables[v], constraint.values[v], `constraint-${cIx}-${v}`)}</div></div>`;

                constraintListHtml += `
                    <div class='constraint' id='constraint-${cIx}' data-index='${cIx}'>
                        <div class='name'>${constraintTypes[constraint.type].name}<div class='expand'></div></div>
                        <div class='variables'>${variableHtml}</div>
                    </div>`;
            }
            let constraintList = document.getElementById('constraint-list');
            constraintList.innerHTML = constraintListHtml;
            Array.from(constraintList.querySelectorAll('.constraint')).forEach(constraintDiv =>
            {
                let cIx = constraintDiv.dataset.index | 0;
                let constraint = state.constraints[cIx];

                for (let v of Object.keys(constraintTypes[constraint.type].variables))
                    setVariableEvents(constraintTypes[constraint.type].variables[v], nv => { saveUndo(); constraint.values[v] = nv; updateVisuals({ storage: true, svg: true }); }, `constraint-${cIx}-${v}`);

                setButtonHandler(constraintDiv, ev =>
                {
                    if (ev.target.nodeName === 'INPUT')
                        return true;
                    if (ev.shiftKey && !ev.ctrlKey)
                        selectConstraintRange(lastSelectedConstraint, cIx);
                    else if (ev.ctrlKey && !ev.shiftKey)
                    {
                        if (selectedConstraints.includes(cIx))
                            selectedConstraints.splice(selectedConstraints.indexOf(cIx), 1);
                        else
                        {
                            selectedConstraints.push(cIx);
                            lastSelectedConstraint = cIx;
                        }
                        updateVisuals();
                    }
                    else
                    {
                        selectedConstraints = [cIx];
                        lastSelectedConstraint = cIx;
                        selectedCells = [];
                        updateVisuals();
                    }
                }, ev => ev.target.nodeName === 'INPUT');
                let expander = constraintDiv.querySelector('.expand');
                setButtonHandler(expander, function() { setClass(constraintDiv, 'expanded', !constraintDiv.classList.contains('expanded')); });
                constraintDiv.querySelector('.name').ondblclick = function(ev) { if (ev.target !== expander) setClass(constraintDiv, 'expanded', !constraintDiv.classList.contains('expanded')); };
            });
        }

        for (let cell = 0; cell < 81; cell++)
        {
            // Cell selection
            setClass(document.getElementById(`sudoku-${cell}`), 'highlighted', selectedCells.includes(cell));
            // Givens
            document.getElementById(`sudoku-text-${cell}`).textContent = state.givens[cell] !== null ? state.givens[cell] : '';
        }

        // Constraint selection
        for (let cIx = 0; cIx < state.constraints.length; cIx++)
            setClass(document.getElementById(`constraint-${cIx}`), 'selected', selectedConstraints.includes(cIx));
        // If constraintSelectionUpdated is true, this is done further up in the Blazor callback
        if (!constraintSelectionUpdated)
            updateConstraintSelection();

        // Fix the viewBox
        let puzzleSvg = puzzleDiv.querySelector('svg.puzzle-svg');

        // — move the button row so that it’s below the puzzle
        let buttonRow = puzzleDiv.querySelector('.button-row');
        let sudokuBBox = puzzleDiv.querySelector('.sudoku').getBBox();
        buttonRow.setAttribute('transform', `translate(0, ${Math.max(9, sudokuBBox.y + sudokuBBox.height) + .5})`);

        // — move the global constraints so they’re to the left of the puzzle
        let globalBox = puzzleDiv.querySelector('.global-constraints');
        globalBox.setAttribute('transform', `translate(${sudokuBBox.x - 1.5}, 0)`);

        // — change the viewBox so that it includes everything
        let fullBBox = puzzleDiv.querySelector('.full-puzzle').getBBox();
        puzzleSvg.setAttribute('viewBox', `${fullBBox.x - .1} ${fullBBox.y - .1} ${fullBBox.width + .2} ${fullBBox.height + .5}`);
    }
    updateVisuals({ storage: true, svg: true, ui: true });

    function saveUndo()
    {
        undoBuffer.push(encodeState(state));
        redoBuffer = [];
    }

    function undo()
    {
        if (undoBuffer.length > 0)
        {
            redoBuffer.push(encodeState(state));
            state = decodeState(undoBuffer.pop());
            updateVisuals({ storage: true, svg: true, ui: true });
        }
    }

    function redo()
    {
        if (redoBuffer.length > 0)
        {
            undoBuffer.push(encodeState(state));
            state = decodeState(redoBuffer.pop());
            updateVisuals({ storage: true, svg: true, ui: true });
        }
    }

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

    function setButtonHandler(btn, click, chk)
    {
        btn.onclick = handler(ev => click(ev) || false);
        btn.onmousedown = chk ? handler(ev => chk(ev) || false) : handler(function() { });
    }

    function clearCells()
    {
        if (selectedCells.length > 0 || selectedConstraints.length > 0)
        {
            saveUndo();
            for (let cell of selectedCells)
                state.givens[cell] = null;
            for (let i = state.constraints.length - 1; i >= 0; i--)
                if (selectedConstraints.includes(i))
                    state.constraints.splice(i, 1);
            let anyConstraintsSelected = selectedConstraints.length > 0;
            selectedConstraints = [];
            updateVisuals({ storage: true, svg: anyConstraintsSelected, ui: true });
        }
    }

    setButtonHandler(puzzleDiv.querySelector(`#btn-clear>rect`), function()
    {
        let elem = document.getElementById(`btn-clear`);
        if (!elem.classList.contains('warning'))
        {
            clearCells();
            elem.classList.add('warning');
            puzzleDiv.querySelector(`#btn-clear>text`).textContent = 'Wipe?';
        }
        else
        {
            resetClearButton();
            saveUndo();
            state = {
                givens: Array(81).fill(null),
                constraints: []
            };
            updateVisuals({ storage: true, svg: true, ui: true });
        }
    });

    setButtonHandler(puzzleDiv.querySelector(`#btn-undo>rect`), undo);
    setButtonHandler(puzzleDiv.querySelector(`#btn-redo>rect`), redo);

    function cellLine(what, offset)
    {
        switch (what)
        {
            case 'e': return Array(9).fill(null).map((_, c) => c + 9 * offset);
            case 'w': return Array(9).fill(null).map((_, c) => 8 - c + 9 * offset);
            case 's': return Array(9).fill(null).map((_, c) => offset + 9 * c);
            case 'n': return Array(9).fill(null).map((_, c) => offset + 9 * (8 - c));
            case 'se': return Array(81).fill(null).map((_, c) => c).filter(c => (c % 9) - ((c / 9) | 0) === offset);
            case 'sw': return Array(81).fill(null).map((_, c) => c).filter(c => (c % 9) + ((c / 9) | 0) === offset);
            case 'nw': return Array(81).fill(null).map((_, c) => 80 - c).filter(c => (c % 9) - ((c / 9) | 0) === offset);
            case 'ne': return Array(81).fill(null).map((_, c) => 80 - c).filter(c => (c % 9) + ((c / 9) | 0) === offset);
        }
    }

    let lastCellLineDir = null;
    let lastCellLineCell = null;

    function selectCell(cell, mode)
    {
        if (mode === 'toggle')
        {
            if (selectedCells.length === 1 && selectedCells[0] === cell && selectedConstraints.length === 0)
                selectedCells = [];
            else
            {
                selectedCells = [cell];
                selectedConstraints = [];
            }
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
        lastCellLineDir = null;
        lastCellLineCell = null;
    }

    function selectCellLine(dir)
    {
        console.log([dir, lastCellLineDir]);
        let c = lastCellLineCell !== null ? lastCellLineCell : selectedCells.length === 0 ? 0 : selectedCells[selectedCells.length - 1]
        let cells, nDir = null;
        if ((lastCellLineDir === 's' && dir === 'e') || (dir === 's' && lastCellLineDir === 'e'))
            cells = cellLine('se', (c % 9) - ((c / 9) | 0));
        else if ((lastCellLineDir === 's' && dir === 'w') || (dir === 's' && lastCellLineDir === 'w'))
            cells = cellLine('sw', (c % 9) + ((c / 9) | 0));
        else if ((lastCellLineDir === 'n' && dir === 'w') || (dir === 'n' && lastCellLineDir === 'w'))
            cells = cellLine('nw', (c % 9) - ((c / 9) | 0));
        else if ((lastCellLineDir === 'n' && dir === 'e') || (dir === 'n' && lastCellLineDir === 'e'))
            cells = cellLine('ne', (c % 9) + ((c / 9) | 0));
        else
        {
            cells = cellLine(dir, (dir === 'n' || dir === 's') ? (c % 9) : ((c / 9) | 0));
            nDir = dir;
        }
        lastCellLineDir = nDir;
        selectedCells = cells;
        selectedConstraints = [];
        lastCellLineCell = c;
        updateVisuals();
    }

    function selectTab(tab)
    {
        Array.from(document.querySelectorAll('.sidebar>.tabs>.tab')).forEach(t => t.classList.remove('active'));
        document.querySelector(`.sidebar>.tabs>.tab-${tab}`).classList.add('active');

        Array.from(document.querySelectorAll('.sidebar>.tabc')).forEach(t => t.classList.remove('active'));
        document.querySelector(`#tab-${tab}`).classList.add('active');

        sidebarDiv.focus();
    }

    Array.from(document.querySelectorAll('.sidebar>.tabs>.tab')).forEach(tab => setButtonHandler(tab, function() { selectTab(tab.dataset.tab); }));
    selectTab('puzzle');
    puzzleContainer.focus();

    function setGiven(digit)
    {
        if (selectedCells.every(c => state.givens[c] === digit))
            return;
        saveUndo();
        for (let cell of selectedCells)
            state.givens[cell] = digit;
        updateVisuals({ storage: true });
    }

    Array.from(document.querySelectorAll('.given-btn')).forEach(btn => { setButtonHandler(btn, function() { setGiven(btn.dataset.given); }); });

    Array.from(document.querySelectorAll('.multi-select')).forEach(btn =>
    {
        btn.onclick = function(ev)
        {
            let cells = cellLine(btn.dataset.what, btn.dataset.offset | 0);
            if (ev.shiftKey && !ev.ctrlKey)
            {
                if (cells.every(c => selectedCells.includes(c)))
                {
                    for (let cell of cells)
                        selectedCells.splice(selectedCells.indexOf(cell), 1);
                }
                else
                {
                    for (let cell of cells)
                        if (!selectedCells.includes(cell))
                            selectedCells.push(cell);
                }
            }
            else
            {
                selectedCells = cells;
                selectedConstraints = [];
            }
            updateVisuals();
        }
    });

    let keepMove = false;
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
            if (selectedCells.length === 0)
                selectedCells = [0];
            else
            {
                let lastCell = lastCellLineCell !== null ? lastCellLineCell : selectedCells[selectedCells.length - 1];
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
                setGiven(parseInt(str.substr(str.length - 1)));
                break;

            case 'Delete':
                clearCells();
                break;

            // Navigation
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
                if (selectedCells.length >= 2 && selectedCells[selectedCells.length - 2] === selectedCells[selectedCells.length - 1])
                    selectedCells.splice(selectedCells.length - 1, 1);
                else
                    keepMove = !keepMove;
                updateVisuals();
                break;
            case 'Ctrl+Shift+ArrowUp': selectCellLine('n'); break;
            case 'Ctrl+Shift+ArrowDown': selectCellLine('s'); break;
            case 'Ctrl+Shift+ArrowLeft': selectCellLine('w'); break;
            case 'Ctrl+Shift+ArrowRight': selectCellLine('e'); break;

            case 'Escape': selectedCells = []; selectedConstraints = []; updateVisuals(); break;
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

    sidebarDiv.addEventListener("keydown", ev =>
    {
        if (ev.target !== sidebarDiv)
            return true;

        let str = ev.code;
        if (ev.shiftKey)
            str = `Shift+${str}`;
        if (ev.altKey)
            str = `Alt+${str}`;
        if (ev.ctrlKey)
            str = `Ctrl+${str}`;

        let anyFunction = true;

        switch (str)
        {
            case 'Delete':
                clearCells();
                break;

            // Navigation
            case 'ArrowUp': selectConstraint(selectedConstraints.length === 0 ? 0 : lastSelectedConstraint > 0 ? lastSelectedConstraint - 1 : lastSelectedConstraint); break;
            case 'ArrowDown': selectConstraint(selectedConstraints.length === 0 ? 0 : lastSelectedConstraint < state.constraints.length - 1 ? lastSelectedConstraint + 1 : lastSelectedConstraint); break;
            case 'Shift+ArrowUp':
                let otherEndUp = selectedConstraints.every(c => c <= lastSelectedConstraint) ? Math.min(...selectedConstraints) : Math.max(...selectedConstraints);
                selectConstraintRange(lastSelectedConstraint, Math.max(0, otherEndUp - 1));
                break;
            case 'Shift+ArrowDown':
                let otherEndDown = selectedConstraints.every(c => c <= lastSelectedConstraint) ? Math.min(...selectedConstraints) : Math.max(...selectedConstraints);
                selectConstraintRange(lastSelectedConstraint, Math.min(state.constraints.length, otherEndDown + 1));
                break;
            case 'Home': selectConstraint(0); break;
            case 'End': selectConstraint(state.constraints.length - 1); break;
            case 'Shift+Home': selectConstraintRange(lastSelectedConstraint, 0); break;
            case 'Shift+End': selectConstraintRange(lastSelectedConstraint, state.constraints.length - 1); break;

            case 'ArrowRight': selectedConstraints.forEach(cIx => { setClass(document.getElementById(`constraint-${cIx}`), 'expanded', true); }); break;
            case 'ArrowLeft': selectedConstraints.forEach(cIx => { setClass(document.getElementById(`constraint-${cIx}`), 'expanded', false); }); break;

            case 'Escape': selectedCells = []; selectedConstraints = []; updateVisuals(); break;
            case 'Ctrl+KeyA': selectedConstraints = state.constraints.map((_, c) => c); updateVisuals(); break;

            // Undo/redo
            case 'Backspace':
            case 'Ctrl+KeyZ':
                undo();
                break;

            case 'Shift+Backspace':
            case 'Ctrl+KeyY':
                redo();
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

    puzzleContainer.onmousedown = function(ev)
    {
        if (!ev.shiftKey && !ev.ctrlKey)
        {
            selectedCells = [];
            selectedConstraints = [];
            updateVisuals();
            remoteLog2(`onmousedown puzzleContainer`);
        }
        else
            remoteLog2(`onmousedown puzzleContainer (canceled)`);
    };
});
