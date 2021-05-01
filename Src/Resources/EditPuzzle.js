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

    function inRange(x) { return x >= 0 && x < 9; }
    function adjacent(cell)
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

    function orthogonal(cell)
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
    Blazor.start({})
        .then(() =>
        {
            for (let i = 0; i < blazorQueue.length; i++)
                DotNet.invokeMethodAsync('ZingaWasm', blazorQueue[i][0], ...blazorQueue[i][1]).then(blazorQueue[i][2]);
            blazorQueue = null;
        });

    let puzzleDiv = document.querySelector('div.puzzle');
    let puzzleContainer = puzzleDiv.querySelector('.puzzle-container');
    let sidebarDiv = document.querySelector('div.sidebar');
    let constraintList = document.getElementById('constraint-list');
    let constraintTypes = JSON.parse(puzzleDiv.dataset.constrainttypes || null) || {};

    let draggingMode = null;
    puzzleContainer.onmouseup = handler(puzzleContainer.ontouchend = function(ev)
    {
        if (ev.type !== 'touchend' || ev.touches.length === 0)
            draggingMode = null;
        remoteLog(`${ev.type} puzzleContainer`);
    });

    function makeEmptyState()
    {
        return {
            title: 'Sudoku',
            author: 'unknown',
            rules: '',
            givens: Array(81).fill(null),
            constraints: [],
            customConstraintTypes: []
        };
    }

    let state = makeEmptyState();
    state.title = puzzleDiv.dataset.title || 'Sudoku';
    state.author = puzzleDiv.dataset.author || 'unknown';
    state.rules = puzzleDiv.dataset.rules || '';
    if (puzzleDiv.dataset.constraints)
        state.constraints = JSON.parse(puzzleDiv.dataset.constraints);
    for (let givenInf of JSON.parse(puzzleDiv.dataset.givens || null) || [])
        state.givens[givenInf[0]] = givenInf[1];

    let undoBuffer = [];
    let redoBuffer = [];

    let selectedCells = [];
    let selectedConstraints = [];
    let lastSelectedCell = 0;
    let lastSelectedConstraint = 0;
    let editingConstraintType = null;
    let editingConstraintTypeParameter = null;

    function remoteLog2(msg)
    {
        remoteLog(`${msg} [${selectedCells.join()}] ${draggingMode ?? "null"}`);
    }

    try
    {
        let str = localStorage.getItem(`zinga-edit`);
        try { item = JSON.parse(str); }
        catch { }
        if (item && item.givens && item.constraints)
        {
            state = item;
            if (state.title === undefined || state.title === null) state.title = 'Sudoku';
            if (state.author === undefined || state.author === null) state.author = 'unknown';
            if (state.rules === undefined || state.rules === null) state.author = '';
            if (state.customConstraintTypes === undefined || state.customConstraintTypes === null) state.customConstraintTypes = [];
        }

        let undoB = localStorage.getItem(`zinga-edit-undo`);
        let redoB = localStorage.getItem(`zinga-edit-redo`);

        undoBuffer = undoB ? JSON.parse(undoB) : [];
        redoBuffer = redoB ? JSON.parse(redoB) : [];
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
        editingConstraintType = null;
        selectedCells = [];
        updateVisuals();
    }

    function selectConstraintRange(cIx1, cIx2, updateOpt)
    {
        if (cIx1 <= cIx2)
            selectedConstraints = Array(cIx2 - cIx1 + 1).fill(null).map((_, c) => c + cIx1);
        else
            selectedConstraints = Array(cIx1 - cIx2 + 1).fill(null).map((_, c) => c + cIx2);
        editingConstraintType = null;
        selectedCells = [];
        updateVisuals(updateOpt);
    }

    function getConstraintType(id)
    {
        return id < 0 ? state.customConstraintTypes[~id] : constraintTypes[id];
    }

    // options:
    //  storage (bool)
    //  svg (bool)          — updates constraint SVG in the grid (this involves Blazor)
    //  ui (bool)            — updates constraint UI in the sidebar
    //  metadata (bool) — updates the title / author / rules textboxes
    function updateVisuals(opt)
    {
        // Update localStorage
        if (localStorage && opt && opt.storage)
        {
            localStorage.setItem(`zinga-edit`, JSON.stringify(state));
            localStorage.setItem(`zinga-edit-undo`, JSON.stringify(undoBuffer));
            localStorage.setItem(`zinga-edit-redo`, JSON.stringify(redoBuffer));
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
            // Re-render all constraint SVGs
            constraintSelectionUpdated = true;
            dotNet('RenderConstraintSvgs', [JSON.stringify(constraintTypes), JSON.stringify(state.customConstraintTypes), JSON.stringify(state.constraints), editingConstraintType, editingConstraintTypeParameter], results =>
            {
                let list = JSON.parse(results);
                document.getElementById('constraint-defs').innerHTML = list[0];
                document.getElementById('constraint-svg').innerHTML = list[1];
                document.getElementById('constraint-svg-global').innerHTML = list[2];
                let editingResult = list[3];

                if (editingConstraintTypeParameter !== null)
                {
                    let reportingBox = document.getElementById(`reporting-${editingConstraintTypeParameter}`);
                    if (reportingBox !== null)
                        reportingBox.innerText = JSON.stringify(editingResult);
                }
                updateConstraintSelection();
                fixViewBox();
            });
        }

        function variableUi(type, value, id, kind, cIx)
        {
            switch (type)
            {
                case 'int':
                    return `<input type='number' step='1' id='${id}' value='${value | 0}' />`;
                case 'bool':
                    return `<input type='checkbox' id='${id}'${value ? ` checked='checked'` : ''} />`;
                case 'list(cell)':
                    return `<button type='button' class='mini-btn show' id='${id}-show' title='Show' tabindex='0'></button><button type='button' class='mini-btn set' id='${id}-set' title='Set constraint to the current selection' tabindex='0'></button>`;
                case 'list(list(cell))':
                    return `<button type='button' class='mini-btn show' id='${id}-show' title='Show' tabindex='0'></button><span class='matching-regions-controls' id='${id}-set' data-constraintix='${cIx}'></span>`;
            }

            let listRx = /^list\((.*)\)$/.exec(type);
            if (listRx !== null)
            {
                var uis = [];
                if (!Array.isArray(value))
                    value = [];
                for (let i = 0; i < value.length; i++)
                    uis.push(`<li>${variableUi(listRx[1], value[i], `${id}-${i}`, kind, cIx)}</li>`);
                return `<ul>${uis.join('')}</ul>`;
            }

            return "(not implemented)";
        }

        function setVariableEvents(type, getter, setter, id)
        {
            switch (type)
            {
                case 'int':
                    document.getElementById(id).onchange = function() { setter(document.getElementById(id).value); };
                    return;
                case 'bool':
                    document.getElementById(id).onchange = function() { setter(document.getElementById(id).checked); };
                    return;
                case 'list(cell)':
                    setButtonHandler(document.getElementById(`${id}-show`), function()
                    {
                        selectedCells = [...getter()];
                        selectedConstraints = [];
                        editingConstraintType = null;
                        updateVisuals();
                    });
                    setButtonHandler(document.getElementById(`${id}-set`), function()
                    {
                        setter(selectedCells);
                    });
                    return;
                case 'list(list(cell))':
                    setButtonHandler(document.getElementById(`${id}-show`), function()
                    {
                        selectedCells = [];
                        for (let inner of getter())
                            selectedCells.push(...inner);
                        selectedConstraints = [];
                        editingConstraintType = null;
                        updateVisuals();
                    });
                    let setDiv = document.getElementById(`${id}-set`);
                    setDiv.zingaSetter = setter;
                    setDiv.zingaRegions = getter();
                    return;
            }
        }

        if (opt && opt.ui)
        {
            // List of constraints
            let constraintListHtml = '';
            for (var cIx = 0; cIx < state.constraints.length; cIx++)
            {
                let constraint = state.constraints[cIx];
                let constraintType = getConstraintType(constraint.type);
                let variableHtml = '';
                for (let v of Object.keys(constraintType.variables))
                    variableHtml += `<div class='variable'><div class='name'>${v}</div><div class='value'>${variableUi(constraintType.variables[v], constraint.values[v], `constraint-${cIx}-${v}`, constraintType.kind, cIx)}</div></div>`;

                constraintListHtml += `
                    <div class='constraint${constraint.type < 0 ? ' custom' : ''}${constraint.expanded ? ' expanded' : ''}' id='constraint-${cIx}' data-index='${cIx}'>
                        <div class='name'><span></span><div class='expand'></div><button class='mini-btn merge' title='Merge constraint types (set selected constraints to match this type)'></button></div>
                        <div class='variables'>${variableHtml}</div>
                    </div>`;
            }
            constraintList.innerHTML = constraintListHtml;
            Array.from(constraintList.querySelectorAll('.constraint')).forEach(constraintDiv =>
            {
                let cIx = constraintDiv.dataset.index | 0;
                let constraint = state.constraints[cIx];
                let cType = getConstraintType(constraint.type);

                let specialVariable = getSpecialVariable(cType.kind);
                if (specialVariable[0] !== null)
                {
                    let enforceResult = enforceConstraintKind(cType.kind, constraint.values[specialVariable[0]]);
                    if (!enforceResult.valid)
                    {
                        constraintDiv.classList.add('warning');
                        constraintDiv.querySelector('.name').setAttribute('title', `${enforceResult.message} Either change the “${specialVariable[0]}” property accordingly, or (if the constraint works correctly as it is) change its Kind to “Custom”.`);
                    }
                }

                for (let v of Object.keys(cType.variables))
                    setVariableEvents(
                        cType.variables[v],
                        () => constraint.values[v],
                        nv =>
                        {
                            if (v === getSpecialVariable(cType.kind)[0])
                            {
                                var result = enforceConstraintKind(cType.kind, nv);
                                if (!result.valid)
                                {
                                    alert(result.message);
                                    return false;
                                }
                                nv = result.value;
                                selectedCells = [];
                                selectedConstraints = [cIx];
                                document.getElementById(`constraint-${cIx}`).classList.remove('warning');
                                document.getElementById(`constraint-${cIx}`).querySelector('.name').removeAttribute('title');
                            }
                            saveUndo();
                            constraint.values[v] = nv;
                            for (let cIx of selectedConstraints)
                                if (state.constraints[cIx].type === constraint.type)
                                    state.constraints[cIx].values[v] = nv;
                            updateVisuals({ storage: true, svg: true });
                            return true;
                        },
                        `constraint-${cIx}-${v}`);

                setButtonHandler(constraintDiv, ev =>
                {
                    if (ev.target.nodeName === 'INPUT')
                        return true;
                    sidebarDiv.focus();
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
                        editingConstraintType = null;
                        updateVisuals();
                    }
                    else
                    {
                        selectedConstraints = [cIx];
                        lastSelectedConstraint = cIx;
                        selectedCells = [];
                        editingConstraintType = null;
                        updateVisuals();
                    }
                }, ev => ev.target.nodeName === 'INPUT');

                function toggleExpanded()
                {
                    constraint.expanded = !constraint.expanded;
                    updateVisuals({ storage: true });
                }
                let expander = constraintDiv.querySelector('.expand');
                setButtonHandler(expander, toggleExpanded);
                constraintDiv.querySelector('.name').ondblclick = function(ev) { if (ev.target !== expander) toggleExpanded(); };

                let mergeBtn = constraintDiv.querySelector('.mini-btn.merge');
                setButtonHandler(mergeBtn, () =>
                {
                    console.log(`Selected: [${selectedConstraints.map(c => `#${c}=${state.constraints[c].type}`).join(', ')}]`);
                    console.log(mergeBtn);
                    let targetTypeId = state.constraints[cIx].type;
                    let targetType = getConstraintType(targetTypeId);
                    console.log(`Target ID: ${targetTypeId}`);
                    saveUndo();
                    for (let selCIx of selectedConstraints)
                        if (state.constraints[selCIx].type !== targetTypeId)
                        {
                            console.log(`Setting constraint ${selCIx} from ${state.constraints[selCIx].type} to ${targetTypeId}`);
                            state.constraints[selCIx].type = targetTypeId;
                            for (let varName of Object.keys(state.constraints[selCIx].values))
                                if (!(varName in targetType.variables))
                                    delete state.constraints[selCIx].values[varName];
                            for (let varName of Object.keys(targetType.variables))
                                if (!(varName in state.constraints[selCIx].values))
                                    state.constraints[selCIx].values[varName] = constraint.values[varName];
                        }
                    updateVisuals({ storage: true, ui: true, svg: true });
                });
            });
        }

        let uniqueConstraintTypes = [...new Set(state.constraints.map(c => c.type))];
        uniqueConstraintTypes.sort((a, b) => b - a);
        function getConstraintColor(cType, selected)
        {
            return `hsl(${(220 + 360 / uniqueConstraintTypes.length * uniqueConstraintTypes.indexOf(cType)) % 360}, ${selected ? '80%, 50%' : '80%, 90%'})`;
        }

        function populateVariableUi(type, value, id, kind, cIx)
        {
            switch (type)
            {
                case 'int':
                    document.getElementById(id).value = (value | 0); break;
                case 'bool':
                    document.getElementById(id).checked = !!value; break;
            }
        }

        // If constraintSelectionUpdated is true, this is done further up in the Blazor callback
        if (!constraintSelectionUpdated)
            updateConstraintSelection();
        // Decide whether to show the “select constraints of the same type” button
        let allSimilar = (selectedConstraints.length > 0 && selectedConstraints.every(ix => state.constraints[ix].type === state.constraints[selectedConstraints[0]].type));
        document.getElementById('constraint-select-similar').style.display = allSimilar ? 'block' : 'none';
        document.getElementById('constraint-code-section').style.display = allSimilar ? 'block' : 'none';
        if (allSimilar)
            populateConstraintEditBox(state.constraints[selectedConstraints[0]].type);

        Array.from(constraintList.querySelectorAll('.constraint')).forEach(constraintDiv =>
        {
            let cIx = constraintDiv.dataset.index | 0;
            let constraint = state.constraints[cIx];
            constraintDiv.style.backgroundColor = getConstraintColor(constraint.type, selectedConstraints.includes(cIx));
            constraintDiv.style.color = selectedConstraints.includes(cIx) ? 'white' : 'black';
            let cType = getConstraintType(constraint.type);
            constraintDiv.querySelector('.name>span').innerText = cType.name;
            constraintDiv.querySelector('.mini-btn.merge').style.display = selectedConstraints.length > 1 && !allSimilar && selectedConstraints.includes(cIx) ? 'block' : 'none';
            setClass(constraintDiv, 'expanded', constraint.expanded);
            for (let v of Object.keys(cType.variables))
                populateVariableUi(cType.variables[v], constraint.values[v], `constraint-${cIx}-${v}`, cType.kind, cIx);
        });

        // Constraint UI that has cell region UI
        let matchingRegions = null;
        Array.from(document.querySelectorAll('.matching-regions-controls')).forEach(regCtrl =>
        {
            if (matchingRegions === null)
                matchingRegions = findMatchingRegions(selectedCells);
            let html = '';
            for (let regions of matchingRegions)
                html += `<button type='button' class='mini-btn set' title='Set constraint to a group of ${regions.length} regions' tabindex='0' data-regions='${JSON.stringify(regions)}'>${regions.length}</button>`;
            regCtrl.innerHTML = html;
            let setter = regCtrl.zingaSetter;
            Array.from(regCtrl.querySelectorAll('.set')).forEach(btn =>
            {
                btn.onmouseover = function()
                {
                    dotNet('GenerateOutline', [btn.dataset.regions], svg => { document.getElementById('temp-svg').innerHTML = svg; });
                };
                btn.onmouseout = function()
                {
                    document.getElementById('temp-svg').innerHTML = '';
                };
                setButtonHandler(btn, () =>
                {
                    document.getElementById('temp-svg').innerHTML = '';
                    let regions = JSON.parse(btn.dataset.regions);
                    setter(regions);
                    selectedCells = [];
                    selectedConstraints = [regCtrl.dataset.constraintix | 0];
                    editingConstraintType = null;
                    regCtrl.zingaRegions = regions;
                    updateVisuals();
                });
            });
        });

        for (let cell = 0; cell < 81; cell++)
        {
            // Cell selection
            setClass(document.getElementById(`sudoku-${cell}`), 'highlighted', selectedCells.includes(cell));
            // Givens
            document.getElementById(`sudoku-text-${cell}`).textContent = state.givens[cell] !== null ? state.givens[cell] : '';
        }

        function fixViewBox()
        {
            // Fix the viewBox
            let puzzleSvg = puzzleDiv.querySelector('svg.puzzle-svg');

            // — move the button row so that it’s below the puzzle
            let buttons = document.getElementById('bb-buttons');
            let sudokuBBox = document.getElementById('bb-puzzle-without-global').getBBox();
            buttons.setAttribute('transform', `translate(0, ${Math.max(9, sudokuBBox.y + sudokuBBox.height) + .5})`);

            // — move the global constraints so they’re to the left of the puzzle
            let globalBox = document.getElementById('constraint-svg-global');
            globalBox.setAttribute('transform', `translate(${sudokuBBox.x - 1.5}, 0)`);

            // — change the viewBox so that it includes everything
            let fullBBox = document.getElementById('bb-everything').getBBox();
            puzzleSvg.setAttribute('viewBox', `${fullBBox.x - .1} ${fullBBox.y - .1} ${fullBBox.width + .2} ${fullBBox.height + .5}`);
        }
        fixViewBox();

        // Title/author
        document.querySelector('#topbar>.title').innerText = state.title;
        document.querySelector('#topbar>.author').innerText = `by ${state.author === '' ? 'unknown' : state.author}`;
        document.title = `Editing: ${state.title ?? 'Sudoku'} by ${state.author ?? 'unknown'}`;

        if (opt && opt.metadata)
        {
            document.getElementById('puzzle-title-input').value = state.title;
            document.getElementById('puzzle-author-input').value = state.author;
            document.getElementById('puzzle-rules-input').value = state.rules;
        }
    }
    updateVisuals({ storage: true, svg: true, ui: true, metadata: true });

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
            state = undoBuffer.pop();
            updateVisuals({ storage: true, svg: true, ui: true, metadata: true });
        }
    }

    function redo()
    {
        if (redoBuffer.length > 0)
        {
            undoBuffer.push(state);
            state = redoBuffer.pop();
            updateVisuals({ storage: true, svg: true, ui: true, metadata: true });
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
            editingConstraintType = null;
            for (let i = 0; i < state.customConstraintTypes.length; i++)
                if (state.customConstraintTypes[i] !== null && state.constraints.every(c => c.type !== ~i))
                    state.customConstraintTypes[i] = null;
            updateVisuals({ storage: true, svg: anyConstraintsSelected, ui: anyConstraintsSelected });
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
            state = makeEmptyState();
            updateVisuals({ storage: true, svg: true, ui: true, metadata: true });
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
        }
        else if (mode === 'add')
        {
            let ix = selectedCells.indexOf(cell);
            if (ix !== -1)
                selectedCells.splice(ix, 1);
            selectedCells.push(cell);
        }
        else    // mode === 'move'
        {
            selectedCells.pop();
            selectedCells.push(cell);
        }
        selectedConstraints = [];
        lastCellLineDir = null;
        lastCellLineCell = null;
        lastSelectedCell = cell;
        editingConstraintType = null;
    }

    function selectCellLine(dir)
    {
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
        editingConstraintType = null;
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

    // Title, Author(s), Rules
    document.getElementById('puzzle-title-input').onchange = function() { saveUndo(); state.title = document.getElementById('puzzle-title-input').value; updateVisuals({ storage: true }); };
    document.getElementById('puzzle-author-input').onchange = function() { saveUndo(); state.author = document.getElementById('puzzle-author-input').value; updateVisuals({ storage: true }); };
    document.getElementById('puzzle-rules-input').onchange = function() { saveUndo(); state.rules = document.getElementById('puzzle-rules-input').value; updateVisuals({ storage: true }); };

    // Buttons that place givens in the grid
    Array.from(document.querySelectorAll('.given-btn')).forEach(btn => { setButtonHandler(btn, function() { setGiven(btn.dataset.given); }); });

    // Arrows at the side of the grid allowing instant selection of rows, columns and diagonals
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
            editingConstraintType = null;
            updateVisuals();
        }
    });

    // TEST PUZZLE button
    setButtonHandler(document.getElementById('puzzle-test'), () => { window.open(`${window.location.protocol}//${window.location.host}/test`); });
    // PUBLISH PUZZLE button
    setButtonHandler(document.getElementById('puzzle-save'), () =>
    {
        document.querySelector('.save-section').classList.add('saving');
        let req = new XMLHttpRequest();
        req.open('POST', '/publish', true);
        req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        req.onreadystatechange = function()
        {
            if (req.readyState !== XMLHttpRequest.DONE)
                return;
            if (req.status !== 200)
                alert(`The puzzle could not be published: ${req.responseText}`);
            else
                window.open(`${window.location.protocol}//${window.location.host}/${req.responseText}`);
            document.querySelector('.save-section').classList.remove('saving');
        };
        req.send(`puzzle=${encodeURIComponent(JSON.stringify(state))}`);
    });

    // “Select similar constraints” button
    setButtonHandler(document.getElementById('constraint-select-similar'), () =>
    {
        selectedConstraints = state.constraints.map((cstr, cIx) => cstr.type === state.constraints[selectedConstraints[0]].type ? cIx : null).filter(c => c !== null);
        editingConstraintType = null;
        updateVisuals();
        sidebarDiv.focus();
    });

    function getSpecialVariable(kind)
    {
        switch (kind)
        {
            case 'Path': return ['cells', 'list(cell)']; break;
            case 'Region': return ['cells', 'list(cell)']; break;
            case 'MatchingRegions': return ['cells', 'list(list(cell))']; break;
            case 'RowColumn': return ['cells', 'list(cell)']; break;
            case 'Diagonal': return ['cells', 'list(cell)']; break;
            case 'TwoCells': return ['cells', 'list(cell)']; break;
            case 'FourCells': return ['topleftcell', 'cell']; break;
        }
        return [null, null];
    }

    // “EDIT CONSTRAINT CODE” box
    function populateConstraintEditBox(cTypeId)
    {
        let primitives = 'cell,int,bool,string,decimal'.split(',');

        let cType = getConstraintType(cTypeId);
        document.getElementById('constraint-code-name').value = cType.name;
        document.getElementById('constraint-code-kind').value = cType.kind;
        document.getElementById('constraint-code-logic').value = cType.logic;
        document.getElementById('constraint-code-svg').value = cType.svg;
        document.getElementById('constraint-code-svgdefs').value = cType.svgdefs;
        document.getElementById('constraint-code-preview').value = cType.preview;

        function generateVariablesTable()
        {
            let variableNames = Object.keys(cType.variables);
            let specialVariable = getSpecialVariable(cType.kind)[0];
            variableNames.sort((a, b) => a === specialVariable ? -1 : b === specialVariable ? 1 : a.localeCompare(b));

            document.getElementById('constraint-code-variables').dataset.variables = JSON.stringify(cType.variables);
            document.getElementById('constraint-code-variables').innerHTML = variableNames
                .map(variableName => `<tr data-variablename='${variableName}'${variableName === specialVariable ? " class='fixed'" : ''}><th>${variableName}${variableName === specialVariable ? '' : `<button class='mini-btn remove'></button>`}</th><td></td></tr>`)
                .join('');
            Array.from(document.getElementById('constraint-code-variables').querySelectorAll('tr')).forEach(tr =>
            {
                function dropdown(id)
                {
                    return `<select id='${id}'>${primitives.map(p => `<option value='${p}'>${p}</option>`).join('')}<option value='list'>list of...</option></select>`;
                }

                let variableName = tr.dataset.variablename;

                if (variableName !== specialVariable)
                {
                    // Button to delete a variable
                    setButtonHandler(tr.querySelector('button.remove'), () =>
                    {
                        saveUndo();
                        delete cType.variables[variableName];
                        generateVariablesTable();
                        updateVisuals({ storage: true, ui: true });
                    });

                    tr.querySelector('th').ondblclick = function()
                    {
                        let lastInput = variableName;
                        while (true)
                        {
                            let newName = prompt('Enter the new name for this property:', lastInput);
                            if (newName !== null && newName !== variableName)
                            {
                                lastInput = newName;
                                if (['gw', 'gh', 'allcells'].includes(newName))
                                {
                                    alert('This property name is reserved. Please choose a different name.');
                                    continue;
                                }

                                saveUndo();
                                cType.variables[newName] = cType.variables[variableName];
                                delete cType.variables[variableName];
                                generateVariablesTable();
                                updateVisuals({ storage: true, ui: true });
                                document.getElementById(`constraint-code-variable-${variableName}-0`).focus();
                            }
                            break;
                        }
                    };

                    function generateDropdowns()
                    {
                        let type = cType.variables[variableName];
                        let html = '';
                        let i = 0;
                        let regexResult;
                        while (regexResult = /^list\((.*)\)$/.exec(type))
                        {
                            html += dropdown(`constraint-code-variable-${variableName}-${i}`);
                            i++;
                            type = regexResult[1];
                        }
                        html += dropdown(`constraint-code-variable-${variableName}-${i}`);
                        tr.querySelector('td').innerHTML = html;
                        Array.from(tr.querySelectorAll('select')).forEach((sel, selIx) =>
                        {
                            sel.value = selIx === i ? type : 'list';
                            sel.onchange = function()
                            {
                                saveUndo();
                                if (sel.value === 'list')
                                    cType.variables[variableName] = `${'list('.repeat(selIx + 1)}cell${')'.repeat(selIx + 1)}`;
                                else
                                    cType.variables[variableName] = `${'list('.repeat(selIx)}${sel.value}${')'.repeat(selIx)}`;
                                generateDropdowns();
                                updateVisuals({ storage: true, ui: true });
                            };
                        });
                    }
                    generateDropdowns();
                }
                else
                {
                    tr.querySelector('td').innerHTML = `<span class='fixed'></span>`;
                    tr.querySelector('td>span').innerText = cType.variables[variableName];
                }
            });
        }

        // Button to add a new property
        setButtonHandler(document.getElementById('constraint-code-addvar'), () =>
        {
            saveUndo();
            let i = 1;
            while (`property${i === 1 ? '' : i}` in cType.variables)
                i++;
            cType.variables[`property${i === 1 ? '' : i}`] = 'cell';
            generateVariablesTable();
            updateVisuals({ storage: true, ui: true });
        });

        // Drop-down to change the kind
        document.getElementById('constraint-code-kind').onchange = function()
        {
            saveUndo();
            cType.kind = document.getElementById('constraint-code-kind').value;
            let inf = getSpecialVariable(cType.kind);
            if (inf[0] !== null)
                cType.variables[inf[0]] = inf[1];
            generateVariablesTable();
            updateVisuals({ storage: true, ui: true });
        };

        generateVariablesTable();
    }

    function editConstraintParameter(elem, paramName, getter, setter)
    {
        return function()
        {
            if (selectedConstraints.length === 0)
                return;
            let newValue = elem.value === '' ? null : elem.value;
            let cTypeId = state.constraints[selectedConstraints[0]].type;
            let oldValue = getter(cTypeId);
            if (newValue === oldValue)
                return;

            if (editingConstraintType !== cTypeId || editingConstraintTypeParameter !== paramName)
                saveUndo();

            if (editingConstraintType !== cTypeId)
            {
                // If only some (not all) constraints of the same type are selected, or the constraint type is a public one, create a copy of the constraint type
                let unselectedSameType = state.constraints.map((c, cIx) => c.type === cTypeId && !selectedConstraints.includes(cIx) ? cIx : null).filter(c => c != null);
                if (cTypeId >= 0 || unselectedSameType.length > 0)
                {
                    let cTypeCopy = JSON.parse(JSON.stringify(getConstraintType(cTypeId)));
                    delete cTypeCopy.public;
                    delete cTypeCopy.shortcut;
                    let nIx = state.customConstraintTypes.findIndex(c => c === null);
                    if (nIx === -1)
                    {
                        nIx = state.customConstraintTypes.length;
                        state.customConstraintTypes.push(null);
                    }
                    let newCTypeId = ~nIx;
                    state.customConstraintTypes[nIx] = cTypeCopy;
                    for (let cIx of selectedConstraints)
                        state.constraints[cIx].type = newCTypeId;
                    cTypeId = newCTypeId;
                }
            }

            editingConstraintType = cTypeId;
            editingConstraintTypeParameter = paramName;
            setter(cTypeId, newValue);
            updateVisuals({ storage: true, svg: true });
        };
    }

    // — Expander
    let constraintCodeBox = document.getElementById('constraint-code-section');
    let constraintCodeExpander = constraintCodeBox.querySelector('.expand');
    setButtonHandler(constraintCodeExpander, function() { setClass(constraintCodeBox, 'expanded', !constraintCodeBox.classList.contains('expanded')); });
    setButtonHandler(constraintCodeBox.querySelector('.label'), function() { });
    constraintCodeBox.querySelector('.label').ondblclick = function(ev) { if (ev.target !== constraintCodeExpander) setClass(constraintCodeBox, 'expanded', !constraintCodeBox.classList.contains('expanded')); };

    // — Text boxes
    document.getElementById('constraint-code-name').onkeyup = editConstraintParameter(document.getElementById('constraint-code-name'), 'name', cTypeId => getConstraintType(cTypeId).name, (cTypeId, value) => { getConstraintType(cTypeId).name = value; });
    document.getElementById('constraint-code-logic').onkeyup = editConstraintParameter(document.getElementById('constraint-code-logic'), 'logic', cTypeId => getConstraintType(cTypeId).logic, (cTypeId, value) => { getConstraintType(cTypeId).logic = value; });
    document.getElementById('constraint-code-svg').onkeyup = editConstraintParameter(document.getElementById('constraint-code-svg'), 'svg', cTypeId => getConstraintType(cTypeId).svg, (cTypeId, value) => { getConstraintType(cTypeId).svg = value; });
    document.getElementById('constraint-code-svgdefs').onkeyup = editConstraintParameter(document.getElementById('constraint-code-svgdefs'), 'svgdefs', cTypeId => getConstraintType(cTypeId).svgdefs, (cTypeId, value) => { getConstraintType(cTypeId).svgdefs = value; });
    document.getElementById('constraint-code-preview').onkeyup = editConstraintParameter(document.getElementById('constraint-code-preview'), 'preview', cTypeId => getConstraintType(cTypeId).preview, (cTypeId, value) => { getConstraintType(cTypeId).preview = value; });

    function findMatchingRegions(cells)
    {
        if (cells.length < 2)
            return [];
        let smallestFactor = 2;
        while (cells.length % smallestFactor !== 0)
            smallestFactor++;
        if (smallestFactor === cells.length)
            return [];
        cells = cells.slice(0);
        cells.sort((c1, c2) => c1 - c2);

        function* recurse(regionSoFar, banned)
        {
            if (regionSoFar.length > 1 && cells.length % regionSoFar.length === 0)
            {
                // Check if the remaining cells form regions that match regionSoFar in shape
                let rem = cells.filter(c => !regionSoFar.includes(c));
                rem.sort((c1, c2) => c1 - c2);
                let reg = [...regionSoFar];
                reg.sort((c1, c2) => c1 - c2);
                let regions = [reg];
                while (rem.length > 0)
                {
                    let xOffset = rem[0] % 9 - reg[0] % 9;
                    let yOffset = ((rem[0] / 9) | 0) - ((reg[0] / 9) | 0);
                    if (!reg.every(c => inRange(c % 9 + xOffset) && inRange(((c / 9) | 0) + yOffset) && rem.includes(c + xOffset + 9 * yOffset)))
                        break;
                    regions.push(reg.map(c => c + xOffset + 9 * yOffset));
                    rem = rem.filter(c => !reg.includes(c - xOffset - 9 * yOffset));
                }
                if (rem.length === 0)
                    yield regions;
            }

            if (regionSoFar.length * smallestFactor < cells.length && regionSoFar.length < 11)
                for (let ix = 0; ix < cells.length; ix++)
                {
                    let adj = cells[ix];
                    if (regionSoFar.includes(adj) || banned.includes(adj) || !(regionSoFar.length === 0 || orthogonal(adj).some(orth => regionSoFar.includes(orth))))
                        continue;
                    regionSoFar.push(adj);
                    banned.push(adj);
                    for (let region of recurse([...regionSoFar], [...banned]))
                        yield region;
                    regionSoFar.pop();
                }
        }

        let result = [...recurse([cells[0]], [cells[0]])];
        result.sort((r1, r2) => r1.length - r2.length);
        return result;
    }

    // Return value is either:
    // { valid: false, message: 'error message' }
    // { valid: true, value?: new value (may or may not be the same as the input) (may be a single cell, list, list of lists, or absent) }
    function enforceConstraintKind(kind, value)
    {
        switch (kind)
        {
            case 'Path':
                if (!Array.isArray(value) || value.length < 2)
                    return { valid: false, message: 'This constraint requires at least two cells.' };

                if (Array(value.length - 1).fill(null).some((_, c) => !adjacent(value[c]).includes(value[c + 1])))
                    return { valid: false, message: 'This constraint requires each cell to be adjacent to the previous, forming a path.' };

                return { valid: true, value: value };

            case 'Region':
                if (!Array.isArray(value) || value.length < 1)
                    return { valid: false, message: 'This constraint requires an orthogonally-connected region of cells.' };

                let copy = [...value];
                let region = [copy.pop()];
                while (copy.length > 0)
                {
                    let ix = copy.findIndex(cell => region.some(r => orthogonal(cell).includes(r)));
                    if (ix === -1)
                        return { valid: false, message: 'This constraint requires an orthogonally-connected region of cells.' };
                    region.push(copy[ix]);
                    copy.splice(ix, 1);
                }
                region.sort((c1, c2) => c1 - c2);
                return { valid: true, value: region };

            case 'MatchingRegions':
                if (Array.isArray(value) && value.every(inner => Array.isArray(inner) &&
                    inner.map((v, ix) => v % 9 - value[0][ix] % 9).every(df => df === inner[0] % 9 - value[0][0] % 9) &&
                    inner.map((v, ix) => ((v / 9) | 0) - ((value[0][ix] / 9) | 0)).every(df => df === ((inner[0] / 9) | 0) - ((value[0][0] / 9) | 0))))
                    return { valid: true, value: value };

                if (Array.isArray(value) && value.every(c => typeof c === 'number'))
                {
                    let regions = findMatchingRegions(value);
                    if (regions.length === 0)
                        return { valid: false, message: 'This constraint requires multiple orthogonally-connected regions of cells of the same shape.' };
                    regions.sort((r1, r2) => r1.length - r2.length);
                    return { valid: true, value: regions[0] };
                }

                return { valid: false, message: 'This constraint requires multiple orthogonally-connected regions of cells of the same shape.' };

            case 'RowColumn':
                if (!Array.isArray(value) || value.length < 2)
                    return { valid: false, message: 'Select at least two cells that are in the same row or column.' };

                let row = value.every(c => ((c / 9) | 0) === ((value[0] / 9) | 0)) ? ((value[0] / 9) | 0) : null;
                let col = value.every(c => (c % 9) === (value[0] % 9)) ? (value[0] % 9) : null;
                if (row === null && col === null)
                    return { valid: false, message: 'This constraint requires a row or a column.' };

                return {
                    valid: true,
                    value: value[1] > value[0]
                        ? (row !== null ? Array(9).fill(null).map((_, c) => c + 9 * row) : Array(9).fill(null).map((_, r) => col + 9 * r))
                        : (row !== null ? Array(9).fill(null).map((_, c) => 8 - c + 9 * row) : Array(9).fill(null).map((_, r) => col + 9 * (8 - r)))
                };

            case 'Diagonal':
                if (!Array.isArray(value) || value.length < 2)
                    return { valid: false, message: 'Select at least two cells that are on the same diagonal.' };

                let forward = value.every(c => (c % 9) - ((c / 9) | 0) === (value[0] % 9) - ((value[0] / 9) | 0)) ? (value[0] % 9) - ((value[0] / 9) | 0) : null;
                let backward = value.every(c => (c % 9) + ((c / 9) | 0) === (value[0] % 9) + ((value[0] / 9) | 0)) ? (value[0] % 9) + ((value[0] / 9) | 0) : null;
                if (forward === null && backward === null)
                    return { valid: false, message: 'This constraint requires a diagonal.' };

                return {
                    valid: true,
                    value: Array(81).fill(null).map((_, c) => value[1] > value[0] ? c : 80 - c)
                        .filter(c => forward !== null ? ((c % 9) - ((c / 9) | 0) === forward) : ((c % 9) + ((c / 9) | 0) === backward))
                };

            case 'TwoCells':
                if (!Array.isArray(value) || value.length !== 2 || !orthogonal(value[0]).includes(value[1]))
                    return { valid: false, message: 'Select two cells that are orthogonally adjacent to one another.' };
                return { valid: true, value: [Math.min(...value), Math.max(...value)] };

            case 'FourCells':
                if (typeof value === 'number' && value % 9 >= 0 && value % 9 < 9 - 1 && ((value / 9) | 0) >= 0 && ((value / 9) | 0) < 9 - 1)
                    return { valid: true, value: value };

                if (!Array.isArray(value) || value.length !== 4)
                    return { valid: false, message: 'Select four cells that form a 2×2 square.' };

                let sorted = [...value];
                sorted.sort((c1, c2) => c1 - c2);
                if (sorted[0] % 9 === 9 - 1 || sorted[1] != sorted[0] + 1 || ((sorted[0] / 9) | 0) === 9 - 1 || sorted[2] != sorted[0] + 9 || sorted[3] != sorted[0] + 9 + 1)
                    return { valid: false, message: 'Select four cells that form a 2×2 square.' };

                return { valid: true, value: sorted[0] };

            case 'Custom':
            case 'Global':
                return { valid: true };
        }

        console.error(`Unknown constraint kind: ${kind}`);
        return { valid: true };
    }

    function addConstraintWithShortcut(letter)
    {
        // Find constraint with the right shortcut key
        let sc = Object.keys(constraintTypes).filter(id => constraintTypes[id].shortcut === letter);
        if (sc.length === 0)
            return;

        let cIx = state.constraints.length;
        let cType = constraintTypes[sc[0]];
        let specialVariable = getSpecialVariable(cType.kind);
        let newConstraint = { type: (sc[0] | 0), values: {} };
        if (specialVariable[0] !== null)
        {
            let enforceResult = enforceConstraintKind(cType.kind, selectedCells);
            if (!enforceResult.valid)
            {
                alert(enforceResult.message);
                return;
            }
            newConstraint.values[specialVariable[0]] = enforceResult.value;
        }

        saveUndo();

        state.constraints.push(newConstraint);
        selectConstraintRange(cIx, cIx, { storage: true, svg: true, ui: true });

        let constraintElem = document.getElementById(`constraint-${cIx}`);
        let uiElement = constraintElem.querySelector('input,textarea,select');
        if (uiElement !== null)
        {
            selectTab('constraints');
            setClass(constraintElem, 'expanded', true);
            uiElement.focus();
            if (uiElement.nodeName === 'INPUT' || uiElement.nodeName === 'TEXTAREA')
                uiElement.select();
        }
    }

    puzzleContainer.addEventListener("keyup", ev =>
    {
        if (ev.key === 'Control')
            selectedCells = [...new Set(selectedCells)];
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
            let toSelect = lastSelectedCell;
            if (selectedCells.length !== 0)
            {
                let lastCell = lastCellLineCell !== null ? lastCellLineCell : selectedCells[selectedCells.length - 1];
                let newX = ((lastCell % 9) + 9 + dx) % 9;
                let newY = (((lastCell / 9) | 0) + 9 + dy) % 9;
                toSelect = newX + 9 * newY;
            }
            selectCell(toSelect, mode);
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

            case 'KeyA':
            case 'KeyB':
            case 'KeyC':
            case 'KeyD':
            case 'KeyE':
            case 'KeyF':
            case 'KeyG':
            case 'KeyH':
            case 'KeyI':
            case 'KeyJ':
            case 'KeyK':
            case 'KeyL':
            case 'KeyM':
            case 'KeyN':
            case 'KeyO':
            case 'KeyP':
            case 'KeyQ':
            case 'KeyR':
            case 'KeyS':
            case 'KeyT':
            case 'KeyU':
            case 'KeyV':
            case 'KeyW':
            case 'KeyX':
            case 'KeyY':
            case 'KeyZ':
                if (selectedCells.length > 0)
                    addConstraintWithShortcut(str.substr(str.length - 1).toLowerCase());
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
            case 'Ctrl+Space':
                let numSel = selectedCells.map((c, ix) => c === selectedCells[selectedCells.length - 1] ? ix : null).filter(x => x !== null);
                if (numSel.length === 2)
                    selectedCells.splice(numSel[0], 1);
                else
                    selectedCells.push(selectedCells[selectedCells.length - 1]);
                updateVisuals();
                break;
            case 'Ctrl+Shift+ArrowUp': selectCellLine('n'); break;
            case 'Ctrl+Shift+ArrowDown': selectCellLine('s'); break;
            case 'Ctrl+Shift+ArrowLeft': selectCellLine('w'); break;
            case 'Ctrl+Shift+ArrowRight': selectCellLine('e'); break;

            case 'Escape': selectedCells = []; selectedConstraints = []; editingConstraintType = null; updateVisuals(); break;
            case 'Ctrl+KeyA': selectedCells = Array(81).fill(null).map((_, c) => c); selectedConstraints = []; editingConstraintType = null; updateVisuals(); break;

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
            // Keys that change something
            case 'Delete': clearCells(); break;
            case 'Ctrl+KeyD':
                if (selectedConstraints.length > 0)
                {
                    saveUndo();
                    let oldLength = state.constraints.length;
                    state.constraints.push(...selectedConstraints.map(c => JSON.parse(JSON.stringify(state.constraints[c]))));
                    selectConstraintRange(oldLength, state.constraints.length - 1, { storage: true, ui: true, svg: true });
                }
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

            // Note: we’re using updateVisuals({ storage: true }) so it’ll remember what constraints were expanded, but we don’t use saveUndo() because we really don’t need that to be an undoable step
            case 'ArrowRight': selectedConstraints.forEach(cIx => { state.constraints[cIx].expanded = true; }); updateVisuals({ storage: true }); break;
            case 'ArrowLeft': selectedConstraints.forEach(cIx => { state.constraints[cIx].expanded = false; }); updateVisuals({ storage: true }); break;

            case 'Escape': selectedCells = []; selectedConstraints = []; editingConstraintType = null; updateVisuals(); break;
            case 'Ctrl+KeyA': selectedCells = []; selectedConstraints = state.constraints.map((_, c) => c); editingConstraintType = null; updateVisuals(); break;

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
            selectedConstraints = [];
            editingConstraintType = null;
            updateVisuals();
            remoteLog2(`onmousedown puzzleContainer`);
        }
        else
            remoteLog2(`onmousedown puzzleContainer (canceled)`);
    };
});
