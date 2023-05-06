window.onload = (function()
{
    /// — FUNCTIONS

    // Utility
    function adjacent(cell)
    {
        let list = [], w = state.width;
        let x = cell % w;
        let y = (cell / w) | 0;
        for (let xx = x - 1; xx <= x + 1; xx++)
            if (inRange(xx))
                for (let yy = y - 1; yy <= y + 1; yy++)
                    if (inRange(yy) && (xx != x || yy != y))
                        list.push(xx + w * yy);
        return list;
    }
    function dotNet(method, args, callback)
    {
        if (blazorQueue === null)
            DotNet.invokeMethodAsync('ZingaWasm', method, ...args).then(callback);
        else
            blazorQueue.push([method, args, callback]);
    }
    function findMatchingRegions(cells)
    {
        if (cells.length < 2)
            return [];
        let w = state.width;
        let smallestFactor = cells.length <= 5 ? 1 : 2;
        while (cells.length % smallestFactor !== 0)
            smallestFactor++;
        if (smallestFactor === cells.length)
            return [];
        cells = cells.slice(0);
        cells.sort((c1, c2) => c1 - c2);

        function* recurse(regionSoFar, banned)
        {
            if ((regionSoFar.length > 1 || (regionSoFar.length === 1 && cells.length <= 5)) && cells.length % regionSoFar.length === 0)
            {
                // Check if the remaining cells form regions that match regionSoFar in shape
                let rem = cells.filter(c => !regionSoFar.includes(c));
                rem.sort((c1, c2) => c1 - c2);
                let reg = [...regionSoFar];
                reg.sort((c1, c2) => c1 - c2);
                let regions = [reg];
                while (rem.length > 0)
                {
                    let xOffset = rem[0] % w - reg[0] % w;
                    let yOffset = ((rem[0] / w) | 0) - ((reg[0] / w) | 0);
                    if (!reg.every(c => inRange(c % w + xOffset) && inRange(((c / w) | 0) + yOffset) && rem.includes(c + xOffset + w * yOffset)))
                        break;
                    regions.push(reg.map(c => c + xOffset + w * yOffset));
                    rem = rem.filter(c => !reg.includes(c - xOffset - w * yOffset));
                }
                if (rem.length === 0 && regions.length > 1)
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
            case 'FourCells': return ['cells', 'list(cell)']; break;
        }
        return [null, null];
    }
    function inRange(x) { return x >= 0 && x < state.width; }
    function keyName(ev)
    {
        let str = ev.code;
        if (ev.shiftKey)
            str = `Shift+${str}`;
        if (ev.altKey)
            str = `Alt+${str}`;
        if (ev.ctrlKey)
            str = `Ctrl+${str}`;
        return str;
    }
    function ns(num) { return Array.from({ length: num }, (_, ix) => ix); }
    function orthogonal(cell)
    {
        let list = [], w = state.width;
        let x = cell % w;
        let y = (cell / w) | 0;
        for (let xx = x - 1; xx <= x + 1; xx++)
            if (inRange(xx))
                for (let yy = y - 1; yy <= y + 1; yy++)
                    if (inRange(yy) && (xx == x || yy == y) && (xx != x || yy != y))
                        list.push(xx + w * yy);
        return list;
    }

    // State, editing, constraints
    function createConstraint(cTypeId)
    {
        // Creates a new constraint (from a constraint type ID) and adds it to the puzzle
        let enf = createDefaultConstraint(getConstraintType(cTypeId), cTypeId, selectedCells);
        if (!enf.valid)
        {
            alert(enf.message);
            return;
        }
        addConstraint(enf.constraint);
    }
    function addConstraint(constraint)
    {
        // Adds a constraints to the puzzle that has already been generated (e.g. by the constraint search box). (Use createConstraint() instead to generate a new constraint.)

        saveUndo();
        let cIx = state.constraints.length;
        state.constraints.push(constraint);
        selectConstraintRange(cIx, cIx, { storage: true, svg: true });

        let constraintElem = document.getElementById(`constraint-${cIx}`);
        let uiElement = constraintElem.querySelector('input,textarea,select');
        if (uiElement !== null)
        {
            selectTab('constraints');
            setClass(constraintElem, 'expanded', true);
            constraint.expanded = true;
            uiElement.focus();
            if (uiElement.nodeName === 'INPUT' || uiElement.nodeName === 'TEXTAREA')
                uiElement.select();
        }
    }
    function addConstraintFromSearch(constr, cType)
    {
        constraintTypes[constr.type] = cType;
        document.getElementById('constraint-search').classList.remove('shown');
        addConstraint(constr);
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
            if (anyConstraintsSelected)
            {
                selectedConstraints = [];
                lastSelectedConstraint = null;
                editingConstraintType = null;
                for (let i = 0; i < state.customConstraintTypes.length; i++)
                    if (state.customConstraintTypes[i] !== null && state.constraints.every(c => c.type !== ~i))
                        state.customConstraintTypes[i] = null;
            }
            updateVisuals({ storage: true, svg: anyConstraintsSelected });
        }
    }
    function coerceValue(value, type)
    {
        let result = /^list\((.*)\)$/.exec(type);
        if (result && !Array.isArray(value))
            return [];
        if (result)
            return value.map(val => coerceValue(val, result[1]));
        switch (type)
        {
            case 'cell':
            case 'int': return value | 0;
            case 'string': return `${value ?? ''}`;
            case 'bool': return !!value;
            case 'decimal': return +value;
        }
        return null;
    }
    function createDefaultConstraint(cType, cTypeId, cells)
    {
        // Return value is either:
        // { valid: false, message: 'error message' }
        // { valid: true, constraint: new constraint }

        let newConstraint = { type: cTypeId, values: {} };
        for (let variableName of Object.keys(cType.variables))
            newConstraint.values[variableName] = coerceValue(variableName === 'cells' ? cells : null, cType.variables[variableName]);

        let [spVar, spType] = getSpecialVariable(cType.kind);
        if (spVar !== null)
        {
            let enforceResult = enforceConstraintKind(cType.kind, cells);
            if (!enforceResult.valid)
                return enforceResult;
            newConstraint.values[spVar] = enforceResult.value;
        }

        return { valid: true, constraint: newConstraint };
    }
    function duplicateConstraints()
    {
        if (selectedConstraints.length > 0)
        {
            saveUndo();
            let oldLength = state.constraints.length;
            state.constraints.push(...selectedConstraints.map(c => JSON.parse(JSON.stringify(state.constraints[c]))));
            selectConstraintRange(oldLength, state.constraints.length - 1, { storage: true, svg: true });
        }
    }
    function editConstraintParameter(cTypeId, paramName, setter)
    {
        if (editingConstraintType !== cTypeId || editingConstraintTypeParameter !== paramName)
            saveUndo();

        let newCTypeId = cTypeId;
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
                newCTypeId = ~nIx;
                state.customConstraintTypes[nIx] = cTypeCopy;
                for (let cIx of selectedConstraints)
                    state.constraints[cIx].type = newCTypeId;
            }
        }

        editingConstraintType = newCTypeId;
        editingConstraintTypeParameter = paramName;
        setter(newCTypeId);
        updateVisuals({ storage: true, svg: true });
    }
    function enforceConstraintKind(kind, value)
    {
        let w = state.width, h = state.height;
        // Return value is either:
        // { valid: false, message: 'error message' }
        // { valid: true, value?: new value (may or may not be the same as the input) (may be a single cell, list, list of lists, or absent) }
        switch (kind)
        {
            case 'Path':
                if (!Array.isArray(value) || value.length < 2)
                    return { valid: false, message: 'This constraint requires at least two cells.' };

                if (ns(value.length - 1).some(c => !adjacent(value[c]).includes(value[c + 1])))
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
                    inner.map((v, ix) => v % w - value[0][ix] % w).every(df => df === inner[0] % w - value[0][0] % w) &&
                    inner.map((v, ix) => ((v / w) | 0) - ((value[0][ix] / w) | 0)).every(df => df === ((inner[0] / w) | 0) - ((value[0][0] / w) | 0))))
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
                    return { valid: false, message: 'This constraint requires at least two cells that are in the same row or column.' };

                let row = value.every(c => ((c / w) | 0) === ((value[0] / w) | 0)) ? ((value[0] / w) | 0) : null;
                let col = value.every(c => (c % w) === (value[0] % w)) ? (value[0] % w) : null;
                if (row === null && col === null)
                    return { valid: false, message: 'This constraint requires a row or a column.' };

                return {
                    valid: true,
                    value: value[1] > value[0]
                        ? (row !== null ? ns(w).map(c => c + w * row) : ns(w).map(r => col + w * r))
                        : (row !== null ? ns(w).map(c => w - 1 - c + w * row) : ns(w).map(r => col + w * (w - 1 - r)))
                };

            case 'Diagonal':
                if (!Array.isArray(value) || value.length < 2)
                    return { valid: false, message: 'This constraint requires at least two cells that are on the same diagonal.' };

                let forward = value.every(c => (c % w) - ((c / w) | 0) === (value[0] % w) - ((value[0] / w) | 0)) ? (value[0] % w) - ((value[0] / w) | 0) : null;
                let backward = value.every(c => (c % w) + ((c / w) | 0) === (value[0] % w) + ((value[0] / w) | 0)) ? (value[0] % w) + ((value[0] / w) | 0) : null;
                if (forward === null && backward === null)
                    return { valid: false, message: 'This constraint requires a diagonal.' };

                return {
                    valid: true,
                    value: ns(w * h).map(c => value[1] > value[0] ? c : w * h - 1 - c)
                        .filter(c => forward !== null ? ((c % w) - ((c / w) | 0) === forward) : ((c % w) + ((c / w) | 0) === backward))
                };

            case 'TwoCells':
                if (!Array.isArray(value) || value.length !== 2 || !orthogonal(value[0]).includes(value[1]))
                    return { valid: false, message: 'This constraint requires two cells that are orthogonally adjacent to one another.' };
                return { valid: true, value: value };

            case 'FourCells':
                if (typeof value === 'number' && value % w >= 0 && value % w < w - 1 && ((value / w) | 0) >= 0 && ((value / w) | 0) < state.height - 1)
                    return { valid: true, value: [value, value + 1, value + w + 1, value + w] };

                if (!Array.isArray(value) || value.length !== 4)
                    return { valid: false, message: 'This constraint requires four cells that form a 2×2 square.' };

                let sorted = [...value];
                sorted.sort((c1, c2) => c1 - c2);
                if (sorted[0] % w === w - 1 || sorted[1] != sorted[0] + 1 || ((sorted[0] / w) | 0) === state.height - 1 || sorted[2] != sorted[0] + w || sorted[3] != sorted[0] + w + 1)
                    return { valid: false, message: 'This constraint requires four cells that form a 2×2 square.' };

                return { valid: true, value: [0, 1, 3, 2].map(c => sorted[c]) };

            case 'Custom':
            case 'Global':
                return { valid: true };
        }

        console.error(`Unknown constraint kind: ${kind}`);
        return { valid: true };
    }
    function getConstraintType(id)
    {
        return id < 0 ? state.customConstraintTypes[~id] : constraintTypes[id];
    }
    function makeEmptyState()
    {
        return {
            title: 'Sudoku',
            author: 'unknown',
            rules: '',
            links: [],
            givens: Array(81).fill(null),
            width: 9,
            height: 9,
            regions: ns(9).map(r => ns(9).map(c => 3 * (r % 3) + (c % 3) + 9 * (3 * ((r / 3) | 0) + ((c / 3) | 0)))),
            labels: ns(9).map(v => `${v + 1}`),
            values: ns(9).map(v => v + 1),
            constraints: [],
            customConstraintTypes: []
        };
    }
    function moveConstraints(up)
    {
        let firstSel = Math.min(...selectedConstraints);
        let lastSel = Math.max(...selectedConstraints) + 1;
        let prevSel = state.constraints[lastSelectedConstraint ?? 0];
        // If there is a gap in the selection
        if ((lastSel - firstSel) !== selectedConstraints.length)
        {
            saveUndo();
            // Move the constraints so that the selected constraints are in one block
            let gap = state.constraints.filter((_, cIx) => cIx >= firstSel && cIx < lastSel && !selectedConstraints.includes(cIx));
            let newConstraintList = state.constraints.slice(0, firstSel);
            if (!up)
                newConstraintList.push(...gap);
            newConstraintList.push(...state.constraints.filter((_, cIx) => selectedConstraints.includes(cIx)));
            if (up)
                newConstraintList.push(...gap);
            newConstraintList.push(...state.constraints.slice(lastSel));
            state.constraints = newConstraintList;
            selectedConstraints = ns(selectedConstraints.length).map(c => c + (up ? firstSel : lastSel - selectedConstraints.length));
        }
        else
        {
            // Do nothing if the selection is already at the top/bottom
            if (up ? (firstSel === 0) : (lastSel === state.constraints.length))
                return;
            saveUndo();
            // Move selected constraints up or down one slot as a group
            let movingConstraintIx = up ? firstSel - 1 : lastSel;
            let movingConstraint = state.constraints.splice(movingConstraintIx, 1);
            state.constraints.splice(up ? lastSel - 1 : firstSel, 0, ...movingConstraint);
            selectedConstraints = selectedConstraints.map(c => up ? (c - 1) : (c + 1));
        }
        lastSelectedConstraint = state.constraints.indexOf(prevSel);
        updateVisuals({ storage: true, svg: true });
    }
    function populateConstraintEditBox(cTypeId)
    {
        let primitives = 'cell,int,bool,string,decimal'.split(',');

        let cType = getConstraintType(cTypeId);
        document.getElementById('constraint-code-name').value = cType.name;
        document.getElementById('constraint-code-kind').value = cType.kind;
        document.getElementById('constraint-code-logic').value = cType.logic;
        document.getElementById('constraint-code-svg').value = cType.svg;
        document.getElementById('constraint-code-svgdefs').value = cType.svgdefs;

        function constraintUpdate(newCTypeId, setter)
        {
            for (let i = 0; i < state.constraints.length; i++)
                if (state.constraints[i].type === newCTypeId)
                    setter(state.constraints[i]);
        }

        function generateVariablesTable()
        {
            let variableNames = Object.keys(cType.variables);
            let [spVar, spType] = getSpecialVariable(cType.kind);
            variableNames.sort((a, b) => a === spVar ? -1 : b === spVar ? 1 : a.localeCompare(b));

            document.getElementById('constraint-code-variables').dataset.variables = JSON.stringify(cType.variables);
            document.getElementById('constraint-code-variables').innerHTML = variableNames
                .map(variableName => `<tr data-variablename='${variableName}'${variableName === spVar ? " class='fixed'" : ''}><th>${variableName}${variableName === spVar ? '' : `<button class='mini-btn remove'></button>`}</th><td></td></tr>`)
                .join('');
            Array.from(document.getElementById('constraint-code-variables').querySelectorAll('tr')).forEach(tr =>
            {
                function dropdown(id)
                {
                    return `<select id='${id}'>${primitives.map(p => `<option value='${p}'>${p}</option>`).join('')}<option value='list'>list of...</option></select>`;
                }

                let variableName = tr.dataset.variablename;

                if (variableName !== spVar)
                {
                    // Button to delete a property
                    setButtonHandler(tr.querySelector('button.remove'), () =>
                    {
                        editConstraintParameter(cTypeId, `variables-${variableName}-delete`, newCTypeId =>
                        {
                            let newCType = getConstraintType(newCTypeId);
                            delete newCType.variables[variableName];
                            constraintUpdate(newCTypeId, c => { delete c.values[variableName]; });
                            generateVariablesTable();
                        });
                    });

                    // Double-click to rename a property
                    tr.querySelector('th').ondblclick = function()
                    {
                        let lastInput = variableName;
                        while (true)
                        {
                            let newName = prompt('Enter the new name for this property:', lastInput);
                            if (newName === null || newName === variableName)
                                break;

                            lastInput = newName;
                            if (['width', 'height', 'allcells', 'true', 'false'].includes(newName))
                            {
                                alert('This property name is reserved. Please choose a different name.');
                                continue;
                            }

                            editConstraintParameter(cTypeId, `variables-${variableName}-rename`, newCTypeId =>
                            {
                                let newCType = getConstraintType(newCTypeId);
                                newCType.variables[newName] = newCType.variables[variableName];
                                delete newCType.variables[variableName];
                                constraintUpdate(newCTypeId, c =>
                                {
                                    c.values[newName] = c.values[variableName];
                                    delete c.values[variableName];
                                });
                                generateVariablesTable();
                                document.getElementById(`constraint-code-variable-${newName}-0`).focus();
                            });
                            break;
                        }
                    };

                    // Drop-downs for the property types
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
                        // Change the type of a property
                        sel.value = selIx === i ? type : 'list';
                        sel.onchange = function()
                        {
                            editConstraintParameter(cTypeId, `variables-${variableName}-type`, newCTypeId =>
                            {
                                let newCType = getConstraintType(newCTypeId);
                                let newType = (sel.value === 'list')
                                    ? `${'list('.repeat(selIx + 1)}cell${')'.repeat(selIx + 1)}`
                                    : `${'list('.repeat(selIx)}${sel.value}${')'.repeat(selIx)}`;
                                newCType.variables[variableName] = newType;
                                constraintUpdate(newCTypeId, c => { c.values[variableName] = coerceValue(c.values[variableName], newType); });
                            });
                        };
                    });
                }
                else
                {
                    tr.querySelector('td').innerHTML = `<span class='fixed'></span> (fixed)`;
                    tr.querySelector('td>span').innerText = cType.variables[variableName];
                }
            });
        }

        // Button to add a new property
        setButtonHandler(document.getElementById('constraint-code-addvar'), () =>
        {
            editConstraintParameter(cTypeId, `variables-addvar`, newCTypeId =>
            {
                let newCType = getConstraintType(newCTypeId), i = 1, varName = 'property';
                while (varName in cType.variables)
                    varName = `property${++i}`;
                newCType.variables[varName] = 'int';
                constraintUpdate(newCTypeId, c => { c.values[varName] = coerceValue(0, 'int'); });
                generateVariablesTable();
            });
        });

        generateVariablesTable();
        updateConstraintErrorReports();
    }
    function setGiven(digit)
    {
        if (!state.values.includes(digit) || selectedCells.every(c => state.givens[c] === digit))
            return;
        saveUndo();
        for (let cell of selectedCells)
            state.givens[cell] = digit;
        updateVisuals({ storage: true });
    }

    // Undo / redo
    function saveUndo()
    {
        undoBuffer.push(JSON.parse(JSON.stringify(state)));
        redoBuffer = [];
    }
    function upgrade(st)
    {
        if (st.width === null || st.width === undefined || st.height === null || st.height === undefined || st.regions === null || st.regions === undefined
            || st.labels === null || st.labels === undefined || st.values === null || st.values === undefined)
        {
            st.width = 9;
            st.height = 9;
            st.regions = ns(9).map(r => ns(9).map(c => 3 * (r % 3) + (c % 3) + 9 * (3 * ((r / 3) | 0) + ((c / 3) | 0))));
            st.labels = ns(9).map(v => `${v + 1}`);
            st.values = ns(9).map(v => v + 1);
        }
        if (st.width < 1)
            st.width = 1;
        if (st.height < 1)
            st.height = 1;
        return st;
    }
    function undo()
    {
        if (undoBuffer.length > 0)
        {
            redoBuffer.push(state);
            state = upgrade(undoBuffer.pop());
            selectedConstraints = selectedConstraints.filter(sc => sc >= 0 && sc < state.constraints.length);
            lastSelectedConstraint = null;
            updateVisuals({ storage: true, svg: true, metadata: true });
        }
    }
    function redo()
    {
        if (redoBuffer.length > 0)
        {
            undoBuffer.push(state);
            state = upgrade(redoBuffer.pop());
            selectedConstraints = selectedConstraints.filter(sc => sc >= 0 && sc < state.constraints.length);
            lastSelectedConstraint = null;
            updateVisuals({ storage: true, svg: true, metadata: true });
        }
    }

    // Selection
    function cellLine(what, offset)
    {
        let w = state.width, h = state.height;
        switch (what)
        {
            case 'e': return ns(w).map(c => c + w * offset);
            case 'w': return ns(w).map(c => w - 1 - c + w * offset);
            case 's': return ns(h).map(c => offset + w * c);
            case 'n': return ns(h).map(c => offset + w * (h - 1 - c));
            case 'se': return ns(w * h).map(c => c).filter(c => (c % w) - ((c / w) | 0) === offset);
            case 'sw': return ns(w * h).map(c => c).filter(c => (c % w) + ((c / w) | 0) === offset);
            case 'nw': return ns(w * h).map(c => w * h - 1 - c).filter(c => (c % w) - ((c / w) | 0) === offset);
            case 'ne': return ns(w * h).map(c => w * h - 1 - c).filter(c => (c % w) + ((c / w) | 0) === offset);
        }
    }
    function selectCell(cell, mode)
    {
        // Processes a mouse click or keypress that causes a cell selection. The cell may be added to an already existing selection.
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
        // Selects one of the rows, columns or diagonals when the user presses the arrow selection thingie at the edge of the grid
        let c = lastCellLineCell !== null ? lastCellLineCell : selectedCells.length === 0 ? 0 : selectedCells[selectedCells.length - 1];
        let cells, nDir = null, w = state.width;
        if ((lastCellLineDir === 's' && dir === 'e') || (dir === 's' && lastCellLineDir === 'e'))
            cells = cellLine('se', (c % w) - ((c / w) | 0));
        else if ((lastCellLineDir === 's' && dir === 'w') || (dir === 's' && lastCellLineDir === 'w'))
            cells = cellLine('sw', (c % w) + ((c / w) | 0));
        else if ((lastCellLineDir === 'n' && dir === 'w') || (dir === 'n' && lastCellLineDir === 'w'))
            cells = cellLine('nw', (c % w) - ((c / w) | 0));
        else if ((lastCellLineDir === 'n' && dir === 'e') || (dir === 'n' && lastCellLineDir === 'e'))
            cells = cellLine('ne', (c % w) + ((c / w) | 0));
        else
        {
            cells = cellLine(dir, (dir === 'n' || dir === 's') ? (c % w) : ((c / w) | 0));
            nDir = dir;
        }
        lastCellLineDir = nDir;
        lastCellLineCell = c;
        selectCells(cells);
    }
    function selectCells(cells)
    {
        // Changes the current selection of cells to a specific set of cells.
        selectedCells = cells.slice(0);
        lastSelectedCell = selectedCells.length > 0 ? Math.min(...selectedCells) : 0;
        selectedConstraints = [];
        editingConstraintType = null;
        updateVisuals();
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
            selectedConstraints = ns(cIx2 - cIx1 + 1).map(c => c + cIx1);
        else
            selectedConstraints = ns(cIx1 - cIx2 + 1).map(c => c + cIx2);
        lastSelectedConstraint = selectedConstraints.length > 0 ? selectedConstraints[0] | 0 : lastSelectedConstraint;
        editingConstraintType = null;
        selectedCells = [];
        updateVisuals(updateOpt);
    }

    // UI
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
    function pressEscape()
    {
        if (document.getElementById('constraint-search').classList.contains('shown'))
        {
            document.getElementById('constraint-search').classList.remove('shown');
            puzzleContainer.focus();
        }
        else
        {
            selectedCells = [];
            selectedConstraints = [];
            editingConstraintType = null;
            updateVisuals();
        }
    }
    function resetClearButton()
    {
        document.getElementById(`btn-clear`).classList.remove('warning');
        document.querySelector(`#btn-clear>text`).textContent = 'Delete';
    }
    function runConstraintSearch(keywords)
    {
        let req = new XMLHttpRequest();
        req.open('POST', '/constraint-search', true);
        req.responseType = 'json';
        req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        req.onerror = () => { console.log(req, arguments); };
        req.onload = () =>
        {
            if (req.status !== 200)
                console.log(req, arguments);
            else if (req.response && req.response.status === 'ok' && Array.isArray(req.response.order))
            {
                constraintResultsSel = null;

                // Create a constraint object for each constraint to determine which ones are valid for the current selection
                let constrPrelimInfos = req.response.order.map(cTypeId =>
                {
                    let cType = req.response.results[cTypeId];
                    let enf = createDefaultConstraint(cType, cTypeId, selectedCells);
                    return { cTypeId: cTypeId, cType: cType, enf: enf };
                });
                // Sort the invalid ones to the end
                constrPrelimInfos.sort((a, b) => a.enf.valid ? b.enf.valid ? 0 : -1 : b.enf.valid ? 1 : 0);

                // Generate DOM elements
                document.getElementById('constraint-results-box').innerHTML = constrPrelimInfos.map((obj, ix) => `
                    <div class='item${obj.enf.valid ? " valid" : ""}' data-ix='${ix}'>
                        <svg viewBox='0 0 10 10' text-anchor='middle' font-family='Bitter'><circle cx='5' cy='5' r='3' stroke-width='.5' stroke='black' fill='#8df' /></svg>
                        <div class='name'></div><div class='akas'></div><div class='descr'></div><div class='error'></div>
                    </div>
                `).join('');

                let constrInfos = Array.from(document.getElementById('constraint-results-box').querySelectorAll('.item')).map(div =>
                {
                    let dat = constrPrelimInfos[div.dataset.ix | 0];
                    let constr = dat.enf.valid ? dat.enf.constraint : null;

                    // Set HTML text
                    div.querySelector('.name').innerText = dat.cType.name;
                    if (dat.cType.akas)
                        div.querySelector('.akas').innerText = `a.k.a.: ${dat.cType.akas.join(', ')}`;
                    div.querySelector('.descr').innerText = dat.cType.description;
                    if (!dat.enf.valid)
                        div.querySelector('.error').innerText = dat.enf.message;

                    // Double-click event
                    if (dat.enf.valid)
                        div.ondblclick = function() { addConstraintFromSearch(constr, dat.cType); };

                    return { constraint: constr, div: div, cType: dat.cType, cTypeId: dat.cTypeId };
                });

                // Render SVGs (for the valid constraints only)
                constraintResults = constrInfos.filter(inf => inf.constraint !== null);
                let w = state.width;
                let minX = Math.min(...selectedCells.map(sc => sc % w));
                let maxX = Math.max(...selectedCells.map(sc => sc % w)) + 1;
                let minY = Math.min(...selectedCells.map(sc => (sc / w) | 0));
                let maxY = Math.max(...selectedCells.map(sc => (sc / w) | 0)) + 1;
                dotNet('RenderConstraintSvgs', [JSON.stringify(req.response.results), JSON.stringify({ customConstraintTypes: [], width: state.width, height: state.height, constraints: constraintResults.map(inf => inf.constraint) })], resultsRaw =>
                {
                    let results = JSON.parse(resultsRaw);
                    for (let i = 0; i < results.svgs.length; i++)
                    {
                        let cTypeId = constraintResults[i].cTypeId;
                        let svg = i === 0 && results.svgDefs !== '' ? `<defs id='constraint-search-result-defs'>${results.svgDefs}</defs>` : '';
                        svg += `<g id='constraint-search-result-svg-${cTypeId}'>${results.svgs[i].svg ?? ''}</g>`;
                        let svgElem = constraintResults[i].div.querySelector('svg');
                        svgElem.innerHTML = svg;
                        let bBox = document.getElementById(`constraint-search-result-svg-${cTypeId}`).getBBox();

                        if (minX < bBox.x)
                        {
                            bBox.width += (bBox.x - minX);
                            bBox.x = minX;
                        }
                        if (maxX > bBox.x + bBox.width)
                            bBox.width += maxX - (bBox.x + bBox.width);

                        if (minY < bBox.y)
                        {
                            bBox.height += (bBox.y - minY);
                            bBox.y = minY;
                        }
                        if (maxY > bBox.y + bBox.height)
                            bBox.height += maxY - (bBox.y + bBox.height);

                        let tall = bBox.height > bBox.width;
                        let x = tall ? bBox.x + bBox.width / 2 - bBox.height / 2 : bBox.x;
                        let y = tall ? bBox.y : bBox.y + bBox.height / 2 - bBox.width / 2;
                        let size = Math.max(bBox.width, bBox.height);
                        svgElem.setAttribute('viewBox', `${x - .1} ${y - .1} ${size + .2} ${size + .2}`);
                    }
                });
            }
        };
        req.send(`msg=${encodeURIComponent(keywords)}`);
    }
    function selectTab(tab)
    {
        Array.from(document.querySelectorAll('#sidebar>.tabs>.tab')).forEach(t => t.classList.remove('active'));
        document.querySelector(`#sidebar>.tabs>.tab-${tab}`).classList.add('active');

        Array.from(document.querySelectorAll('#sidebar>.tabc')).forEach(t => t.classList.remove('active'));
        document.querySelector(`#tab-${tab}`).classList.add('active');

        sidebarDiv.focus();
    }
    function setButtonHandler(btn, click, chk)
    {
        btn.onclick = handler(ev => click(ev) || false);
        btn.onmousedown = chk ? handler(ev => chk(ev) || false) : handler(function() { });
    }
    function setClass(elem, className, setUnset)
    {
        if (setUnset)
            elem.classList.add(className);
        else
            elem.classList.remove(className);
    }
    function setCellSelectionEvents()
    {
        Array.from(puzzleContainer.getElementsByClassName('sudoku-cell')).forEach(cellRect =>
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
    }
    function setConstraintCodeEditingEvent(id, setEvent, getter, setter)
    {
        let elem = document.getElementById(`constraint-code-${id}`);
        setEvent(elem, () =>
        {
            let cTypeId = state.constraints[selectedConstraints[0]].type;
            if (elem.value !== getter(cTypeId))
                editConstraintParameter(cTypeId, id, cTypeId => setter(cTypeId, elem.value));
        });
    }
    function updateConstraintErrorReports()
    {
        for (let prm of ['svg', 'svgdefs', 'logic'])
        {
            let reportingBox = document.getElementById(`reporting-${prm}`);
            let editingBox = document.getElementById(`constraint-code-${prm}`);
            let errorConstraints = selectedConstraints.filter(c => c >= 0 && c < constraintErrors.length && prm in constraintErrors[c]);
            if (editingBox.value.trim().length === 0 || errorConstraints.length === 0)
                reportingBox.classList.remove('has-error');
            else
            {
                let err = constraintErrors[errorConstraints[0]][prm];
                reportingBox.innerHTML = `<span class='error'></span>
                    ${err.start && err.end ? `<a class='show' href='#' data-start='${err.start}' data-end='${err.end}'>show</a>` : ''}
                    ${'highlights' in err ? err.highlights.map(hl => ` <a class='show' href='#' data-start='${hl.start}' data-end='${hl.end}'>show</a>`).join('') : ''}`;
                reportingBox.querySelector('span.error').innerText = err.msg;
                Array.from(reportingBox.querySelectorAll('a.show')).forEach(a =>
                {
                    setButtonHandler(a, function()
                    {
                        if (a.dataset.start && a.dataset.end)
                            editingBox.setSelectionRange(a.dataset.start | 0, a.dataset.end | 0);
                    });
                });
                reportingBox.classList.add('has-error');
            }
        }
    }
    function updateVisuals(opt)
    {
        // options:
        //  storage (bool)    — updates localStorage with the current state and the undo/redo history
        //  svg (bool)          — updates constraint SVG in the grid (this involves Blazor)
        //  metadata (bool) — updates the title / author / rules / links section

        let w = state.width, h = state.height;

        // Update localStorage
        if (localStorage && opt && opt.storage)
        {
            localStorage.setItem(`zinga-edit`, JSON.stringify(state));
            localStorage.setItem(`zinga-edit-undo`, JSON.stringify(undoBuffer));
            localStorage.setItem(`zinga-edit-redo`, JSON.stringify(redoBuffer));
        }

        function updateConstraintAndCellSelection()
        {
            // Constraint selection
            for (let cIx = 0; cIx < state.constraints.length; cIx++)
            {
                if (selectedConstraints.includes(cIx))
                    document.getElementById(`constraint-svg-${cIx}`).classList.add('selected');
                else
                    document.getElementById(`constraint-svg-${cIx}`).classList.remove('selected');
            }

            // Cell selection and givens
            for (let cell = 0; cell < w * h; cell++)
            {
                setClass(document.getElementById(`sudoku-${cell}`), 'highlighted', selectedCells.includes(cell));
                document.getElementById(`sudoku-text-${cell}`).textContent = state.givens[cell] !== null ? state.givens[cell] : '';
            }
        }

        function updateConstraintErrors(constraintDiv, cIx)
        {
            if (constraintErrors[cIx] && Object.keys(constraintErrors[cIx]).length)
            {
                constraintDiv.classList.add('warning');
                constraintDiv.querySelector('.name').setAttribute('title', Object.keys(constraintErrors[cIx]).map(key => constraintErrors[cIx][key].msg).join(' // '));
            }
        }

        let constraintSelectionUpdated = false;
        if (opt && opt.svg)
        {
            // Button row below the puzzle grid
            let buttonRows = [[
                { id: 'btn-clear', text: 'Delete' },
                { id: 'btn-undo', text: 'Undo' },
                { id: 'btn-redo', text: 'Redo' }
            ]];
            let y = 0, btnSvg = '', btnHeight = .8, btnMargin = .135, btnPadding = .135;
            for (let row of buttonRows)
                for (let btnInfo of row)
                    btnSvg += `
                        <g class='button' id='${btnInfo.id}'>
                            <rect class='clickable' x='0' y='0' width='1' height='${btnHeight}' stroke-width='.025' rx='.08' ry='.08'/>
                            <text class='label'>${btnInfo.text}</text>
                        </g>`;
            document.getElementById('bb-buttons-scaler').innerHTML = btnSvg;
            document.getElementById('bb-buttons-scaler').removeAttribute('transform');

            for (let rowIx = 0; rowIx < buttonRows.length; rowIx++)
            {
                let row = buttonRows[rowIx];
                for (let btnInfo of row)
                {
                    btnInfo.btn = document.getElementById(btnInfo.id);
                    btnInfo.label = btnInfo.btn.querySelector('text.label');
                    btnInfo.rect = btnInfo.btn.querySelector('rect.clickable');
                    btnInfo.w = btnInfo.label.getBBox().width + 2 * btnPadding;
                }
                console.log(row);
                let totalWidth = row.reduce((p, n) => p + n.w, 0) + (row.length - 1) * btnMargin;
                let extraPadding = totalWidth <= state.width ? (state.width - totalWidth) / row.length : 0;
                let scale = totalWidth <= state.width ? null : state.width / totalWidth;
                let x = 0;
                for (let btnInfo of row)
                {
                    btnInfo.rect.setAttribute('x', x);
                    btnInfo.rect.setAttribute('y', rowIx);
                    btnInfo.rect.setAttribute('width', btnInfo.w + extraPadding);
                    btnInfo.rect.setAttribute('height', btnHeight);
                    btnInfo.label.setAttribute('x', x + (btnInfo.w + extraPadding) / 2);
                    btnInfo.label.setAttribute('y', rowIx + .6);
                    x += btnInfo.w + extraPadding + btnMargin;
                }
                y += 1;
                if (scale !== null)
                    document.getElementById('bb-buttons-scaler').setAttribute('transform', `scale(${scale})`);
            }

            setButtonHandler(document.getElementById('btn-clear'), function()
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
                    updateVisuals({ storage: true, svg: true, metadata: true });
                }
            });
            setButtonHandler(document.getElementById('btn-undo'), undo);
            setButtonHandler(document.getElementById('btn-redo'), redo);

            // Constraint SVGs
            constraintSelectionUpdated = true;
            dotNet('RenderConstraintSvgs', [JSON.stringify(constraintTypes), JSON.stringify(state)], resultsRaw =>
            {
                let results = JSON.parse(resultsRaw);
                let globalSvgs = '', svgs = '', globalY = 0;
                for (let cIx = 0; cIx < results.svgs.length; cIx++)
                    if (results.svgs[cIx].global)
                    {
                        globalSvgs += `<g class='constraint-svg' id='constraint-svg-${cIx}' transform='translate(0, ${globalY})'>${results.svgs[cIx].svg}</g>`;
                        globalY += 1.5;
                    }
                    else
                        svgs += `<g class='constraint-svg' id='constraint-svg-${cIx}'>${results.svgs[cIx].svg}</g>`;

                document.getElementById('constraint-svg-global').innerHTML = globalSvgs;
                document.getElementById('constraint-svg').innerHTML = svgs;
                document.getElementById('constraint-defs').innerHTML = results.svgDefs;
                document.getElementById('puzzle-frame').setAttribute('d', results.frame);
                document.getElementById('puzzle-lines').setAttribute('d', results.lines);

                document.getElementById('puzzle-cells').innerHTML = ns(w * h).map(cell => `
                    <g class='cell${state.regions.length < 1 || state.regions.some(r => r.includes(cell)) ? '' : ' no-region'}' id='sudoku-${cell}' font-size='.25' stroke-width='0'>
                        <rect class='clickable sudoku-cell' data-cell='${cell}' x='${cell % w}' y='${(cell / w) | 0}' width='1' height='1' />
                        <text id='sudoku-text-${cell}' x='${cell % w + .5}' y='${((cell / w) | 0) + .725}' font-size='.65'></text>
                    </g>`).join('');
                setCellSelectionEvents();

                constraintErrors = results.errors;
                updateConstraintAndCellSelection();
                Array.from(constraintList.querySelectorAll('.constraint')).forEach(constraintDiv => { updateConstraintErrors(constraintDiv, constraintDiv.dataset.index | 0); });
                updateConstraintErrorReports();
                fixViewBox();
            });

            // Givens/values/labels
            let givenPresets = [
                { id: 'n1', name: `1–${w}`, label: n => `${n + 1}`, value: n => n + 1 },
                { id: 'n0', name: `0–${w - 1}`, label: n => `${n}`, value: n => n },
                { id: 'l', name: `A–${String.fromCharCode(64 + w)}`, label: n => String.fromCharCode(65 + n), value: n => n + 1 }
            ];
            document.getElementById('givens-presets').innerHTML = givenPresets.map(p => `<button id='givens-preset-${p.id}'>${p.name}</button>`).join('');
            givenPresets.forEach(preset =>
            {
                document.getElementById(`givens-preset-${preset.id}`).onclick = function()
                {
                    let newLabels = ns(w).map(preset.label);
                    let newValues = ns(w).map(preset.value);
                    let newGivens = state.givens.map(g => g === null || !newValues.includes(g) ? null : g);
                    if (state.labels.length === w && newLabels.every((l, ix) => l === state.labels[ix]) &&
                        state.values.length === w && newValues.every((v, ix) => v === state.values[ix]) &&
                        newGivens.every((g, ix) => g === state.givens[ix]))
                        return;
                    saveUndo();
                    state.labels = newLabels;
                    state.values = newValues;
                    state.givens = newGivens;
                    updateVisuals({ svg: true, storage: true });
                };
            });

            document.getElementById('givens').innerHTML = state.labels.map((lbl, ix) => `
                <div class='given' data-given='${state.values[ix]}'>
                    <div class='given-btns'><button class='given-btn'>${lbl}</button></div>
                    <div><input type='number' value='${state.values[ix]}' /></div>
                    <div><button class='mini-btn remove' /></div>
                </div>
            `).join('');
            Array.from(document.querySelectorAll('#givens .given')).forEach(givenDiv =>
            {
                let givenValue = givenDiv.dataset.given | 0;
                setButtonHandler(givenDiv.querySelector('button.given-btn'), function() { setGiven(givenValue); });
                setButtonHandler(givenDiv.querySelector('button.remove'), function()
                {
                    let ix = state.values.indexOf(givenValue);
                    saveUndo();
                    state.values.splice(ix, 1);
                    state.labels.splice(ix, 1);
                    state.givens = state.givens.map(g => g === null || !state.values.includes(g) ? null : g);
                    updateVisuals({ svg: true, storage: true });
                });
            });

            // Regions
            let regionsHtml = '';
            if (state.regions !== null)
                for (let regionIx = 0; regionIx < state.regions.length; regionIx++)
                {
                    let region = state.regions[regionIx];
                    let xs = region.map(cell => cell % w);
                    let minX = Math.min(...xs);
                    let maxX = Math.max(...xs);
                    let ys = region.map(cell => (cell / w) | 0);
                    let minY = Math.min(...ys);
                    let maxY = Math.max(...ys);
                    regionsHtml += `
                        <div class='region' data-region='${regionIx}'>
                            <div class='region-svg'>
                                <svg viewBox='${minX - .2} ${minY - .2} ${maxX - minX + 1.4} ${maxY - minY + 1.4}'>
                                    <path d='${region.map((_, ix) => `M${xs[ix]} ${ys[ix]}h1v1h-1z`).join('')}' fill='white' stroke='none' />
                                    <use href='#puzzle-frame' />
                                    <use href='#puzzle-lines' />
                                </svg>
                            </div>
                            <div class='controls'>
                                <button class='mini-btn show' title='Select the cells of this region' />
                                <button class='mini-btn set' title='Change this region' />
                                <button class='mini-btn remove' title='Remove this region' />
                            </div>
                        </div>`;
                }
            document.getElementById('regions').innerHTML = regionsHtml;

            Array.from(document.querySelectorAll('#regions .region')).forEach(regionDiv =>
            {
                let regionIx = regionDiv.dataset.region | 0;
                regionDiv.querySelector('.mini-btn.show').onclick = function() { selectCells(state.regions[regionIx]); };
                regionDiv.querySelector('.mini-btn.remove').onclick = function() { saveUndo(); state.regions.splice(regionIx, 1); updateVisuals({ svg: true, storage: true }); };
                regionDiv.querySelector('.mini-btn.set').onclick = function()
                {
                    if (selectedCells.length < 1)
                    {
                        alert('Select a set of cells to change this region.');
                        return;
                    }
                    let cells = [...new Set(selectedCells)];
                    cells.sort((c1, c2) => c1 - c2);
                    if (cells.length === state.regions[regionIx].length && cells.every((c, ix) => c === state.regions[regionIx][ix]))
                        return;
                    saveUndo();
                    state.regions[regionIx] = cells;
                    for (let rg = state.regions.length - 1; rg >= 0; rg--)
                        if (rg !== regionIx)
                        {
                            state.regions[rg] = state.regions[rg].filter(cell => !cells.includes(cell));
                            if (state.regions[rg].length === 0)
                                state.regions.splice(rg, 1);
                        }
                    state.regions.sort((r1, r2) => r1[0] - r2[0]);
                    updateVisuals({ svg: true, storage: true });
                };
            });

            // Find possible cleanly-divisible region sizes
            let regionPresetButtons = '';
            for (var rw = 2; 2 * rw <= w; rw++)
                if (w % rw === 0)
                    for (var rh = 2; 2 * rh <= h; rh++)
                        if (h % rh === 0 && (w * h / rw / rh === w || (w * h / rw / rh) === h))
                            regionPresetButtons += `<button data-rw='${rw}' data-rh='${rh}' class='region-preset'>${rw}×${rh}</button>`;
            document.getElementById('region-presets').innerHTML = regionPresetButtons;
            Array.from(document.querySelectorAll('#region-presets>button.region-preset')).forEach(btn =>
            {
                let rw = btn.dataset.rw | 0, rh = btn.dataset.rh | 0, nx = (w / rw) | 0, ny = (h / rh) | 0;
                btn.onclick = function()
                {
                    let newRegions = [...Array(nx * ny).keys()].map(rg => [...Array(rw * rh).keys()].map(ix => ix % rw + rw * (rg % nx) + w * (((ix / rw) | 0) + rh * ((rg / nx) | 0))));
                    if (newRegions.length === state.regions.length && newRegions.every((rg, rIx) => rg.length === state.regions[rIx].length && rg.every((c, cIx) => c === state.regions[rIx][cIx])))
                        return;
                    saveUndo();
                    state.regions = newRegions;
                    updateVisuals({ storage: true, svg: true });
                };
            });

            setClass(document.getElementById('region-presets'), 'hidden', !regionPresetButtons.length);
            setClass(document.getElementById('region-remove-all'), 'hidden', !state.regions.length);
            setClass(document.getElementById('region-fill'), 'hidden', state.regions.reduce((a, b) => a + b.length, 0) === w * h);

            // Multi-select arrows
            let multiSelectArrows = '';
            for (let col = 0; col < w; col++)
            {
                multiSelectArrows += `<path class='multi-select' data-what='n' data-offset='${col}' d='m ${col + .3} ${h + .3} .2 -.2 .2 .2z' fill='black' />`;
                multiSelectArrows += `<path class='multi-select' data-what='s' data-offset='${col}' d='m ${col + .3} -.3 .2 .2 .2 -.2z' fill='black' />`;
            }
            for (let row = 0; row < h; row++)
            {
                multiSelectArrows += `<path class='multi-select' data-what='e' data-offset='${row}' d='m -.3 ${row + .3} .2 .2 -.2 .2z' fill='black' />`;
                multiSelectArrows += `<path class='multi-select' data-what='w' data-offset='${row}' d='m ${w + .3} ${row + .3} -.2 .2 .2 .2z' fill='black' />`;
            }
            for (let offset = 0; offset < w + h - 1; offset++)
            {
                multiSelectArrows += `<path class='multi-select' data-what='se' data-offset='${offset - h + 1}' d='m ${(offset < h ? 0 : offset - h + 1) - .1} ${(offset >= h ? 0 : h - 1 - offset) - .1} -.2 0 .2 -.2 z' fill='black' />`;
                multiSelectArrows += `<path class='multi-select' data-what='sw' data-offset='${offset}' d='m ${(offset < w ? offset + 1 : w) + .1} ${(offset >= w ? offset - w + 1 : 0) - .1} 0 -.2 .2 .2 z' fill='black' />`;
                multiSelectArrows += `<path class='multi-select' data-what='nw' data-offset='${w - 1 - offset}' d='m ${(offset < h ? w : w + h - 1 - offset) + .1} ${(offset >= h ? h : offset + 1) + .1} .2 0 -.2 .2 z' fill='black' />`;
                multiSelectArrows += `<path class='multi-select' data-what='ne' data-offset='${w + h - 2 - offset}' d='m ${(offset < w ? w - 1 - offset : 0) - .1} ${(offset >= w ? w + h - 1 - offset : h) + .1} -.2 0 .2 .2 z' fill='black' />`;
            }
            document.getElementById('multi-selects').innerHTML = multiSelectArrows;
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
                };
            });
        }

        resetClearButton();

        function variableUi(type, value, id, kind, cIx)
        {
            switch (type)
            {
                case 'int':
                    return `<input type='number' step='1' id='${id}' value='${value | 0}' />`;

                case 'string':
                    return `<input type='text' id='${id}' value='${value}' />`;

                case 'bool':
                    return `<input type='checkbox' id='${id}'${value ? ` checked='checked'` : ''} />`;

                case 'cell':
                case 'list(cell)':
                    return `<button type='button' class='mini-btn show' id='${id}-show' title='Show' tabindex='0'></button><button type='button' class='mini-btn set' id='${id}-set' title='Set constraint to the current selection' tabindex='0'></button>`;

                case 'list(list(cell))':
                    return `<button type='button' class='mini-btn show' id='${id}-show' title='Show' tabindex='0'></button><span class='matching-regions-controls' id='${id}-set' data-constraintix='${cIx}'></span>`;

                case 'list(int)':
                    return `<span class='int-list' id='${id}'>${value.map((v, vIx) => `<input type='number' step='1' value='${v}' id='${id}-${vIx}' data-ix='${vIx}' />`).join('')}<input type='number' step='1' id='${id}-${value.length}' data-ix='${value.length}' /></span>`;
            }

            let listRx = /^list\((.*)\)$/.exec(type);
            if (listRx !== null)
            {
                let uis = [];
                if (!Array.isArray(value))
                    value = [];
                for (let i = 0; i < value.length; i++)
                    uis.push(`<li><button class='mini-btn add' id='${id}-add-${i}' data-ix='${i}'></button><button class='mini-btn remove' id='${id}-remove-${i}' data-ix='${i}'></button>${variableUi(listRx[1], value[i], `${id}-${i}`, kind, cIx)}</li>`);
                return `<ol id='${id}'>${uis.join('')}<li class='extra'><button class='mini-btn add' id='${id}-add-${value.length}' data-ix='${value.length}'></button></li></ol>`;
            }

            return "(not implemented)";
        }

        function setVariableEvents(type, getter, setter, id)
        {
            switch (type)
            {
                case 'int':
                    document.getElementById(id).onchange = function() { setter(document.getElementById(id).value | 0); };
                    return;

                case 'string':
                    document.getElementById(id).onchange = function() { setter(document.getElementById(id).value); };
                    return;

                case 'bool':
                    document.getElementById(id).onchange = function() { setter(document.getElementById(id).checked); };
                    return;

                case 'cell':
                case 'list(cell)':
                    setButtonHandler(document.getElementById(`${id}-show`), function() { selectCells(type === 'cell' ? [getter()] : getter()); });
                    if (type === 'cell')
                        setButtonHandler(document.getElementById(`${id}-set`), function() { if (selectedCells.length > 0) setter(Math.min(...selectedCells)); });
                    else
                        setButtonHandler(document.getElementById(`${id}-set`), function() { setter(selectedCells); });
                    return;

                case 'list(list(cell))':
                    setButtonHandler(document.getElementById(`${id}-show`), function()
                    {
                        selectedCells = [];
                        for (let inner of getter())
                            selectedCells.push(...inner);
                        lastSelectedCell = selectedCells.length > 0 ? Math.min(...selectedCells) : 0;
                        selectedConstraints = [];
                        editingConstraintType = null;
                        updateVisuals();
                    });
                    let setDiv = document.getElementById(`${id}-set`);
                    setDiv.zingaSetter = setter;
                    setDiv.zingaRegions = getter();
                    return;

                case 'list(int)':
                    let outerDiv = document.getElementById(id);
                    function setInputChangeEvent(input)
                    {
                        let ix = input.dataset.ix | 0;
                        input.onchange = function()
                        {
                            let intArray = getter().slice(0);
                            if (input.value === '')
                            {
                                intArray.length = ix;
                                Array.from(outerDiv.querySelectorAll('input')).filter(inp => (inp.dataset.ix | 0) > ix).forEach(inp => { inp.remove(); });
                            }
                            else if (ix === intArray.length)
                            {
                                intArray.push(input.value | 0);
                                let t = document.createElement('div');
                                t.innerHTML = `<input type='number' step='1' id='${id}-${intArray.length}' data-ix='${intArray.length}'>`;
                                t = t.firstChild;
                                outerDiv.appendChild(t);
                                setInputChangeEvent(t);
                            }
                            else
                                intArray[ix] = (input.value | 0);
                            setter(intArray);
                        };
                    }
                    Array.from(outerDiv.querySelectorAll('input')).forEach(input => { setInputChangeEvent(input); });
                    return;
            }

            let listRx = /^list\((.*)\)$/.exec(type);
            if (listRx !== null)
            {
                let elementType = listRx[1];
                Array.from(document.getElementById(id).querySelectorAll(':scope>li>button.remove')).forEach(btn =>
                {
                    let ix = btn.dataset.ix | 0;
                    setButtonHandler(btn, () => { let arr = getter().slice(0); arr.splice(ix, 1); setter(arr); });
                });
                Array.from(document.getElementById(id).querySelectorAll(':scope>li>button.add')).forEach(btn =>
                {
                    let ix = btn.dataset.ix | 0;
                    setButtonHandler(btn, () => { let arr = getter().slice(0); arr.splice(ix, 0, coerceValue(null, elementType)); setter(arr); });
                });
                getter().forEach((_, i) => { setVariableEvents(elementType, () => getter()[i], v => { getter()[i] = v; setter(getter()); }, `${id}-${i}`); });
            }
        }

        // List of constraints
        let constraintListHtml = '';
        for (let cIx = 0; cIx < state.constraints.length; cIx++)
        {
            let constraint = state.constraints[cIx];
            let constraintType = getConstraintType(constraint.type);
            let variableHtml = '';
            for (let v of Object.keys(constraintType.variables))
                variableHtml += `<div class='variable'><div class='name'>${v}</div><div class='value'>${variableUi(constraintType.variables[v], constraint.values[v], `constraint-${cIx}-${v}`, constraintType.kind, cIx)}</div></div>`;

            constraintListHtml += `
                <div class='constraint${constraint.expanded ? ' expanded' : ''}' id='constraint-${cIx}' data-index='${cIx}'>
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

            let [spVar, spType] = getSpecialVariable(cType.kind);
            let enforceResult;
            if (spVar !== null && !(enforceResult = enforceConstraintKind(cType.kind, constraint.values[spVar])).valid)
            {
                constraintDiv.classList.add('warning');
                constraintDiv.querySelector('.name').setAttribute('title', `${enforceResult.message} Either change the “${spVar}” property accordingly, or (if the constraint works correctly as it is) change its Kind to “Custom”.`);
            }

            // If constraintSelectionUpdated is true, this is done further up in the Blazor callback
            if (!constraintSelectionUpdated)
                updateConstraintErrors(constraintDiv, cIx); // Updates the warning symbols in the constraints list
            updateConstraintErrorReports(); // Updates the error boxes inside the “Edit constraint code” box

            for (let v of Object.keys(cType.variables))
                setVariableEvents(
                    cType.variables[v],
                    () => constraint.values[v],
                    nv =>
                    {
                        if (v === getSpecialVariable(cType.kind)[0])
                        {
                            let result = enforceConstraintKind(cType.kind, nv);
                            if (!result.valid)
                            {
                                alert(result.message);
                                return false;
                            }
                            nv = result.value;
                            selectedCells = [];
                            selectedConstraints = [cIx];
                            lastSelectedConstraint = cIx;
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
                    selectConstraintRange(lastSelectedConstraint ?? 0, cIx);
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
                let targetTypeId = state.constraints[cIx].type;
                let targetType = getConstraintType(targetTypeId);
                saveUndo();
                for (let selCIx of selectedConstraints)
                    if (state.constraints[selCIx].type !== targetTypeId)
                    {
                        state.constraints[selCIx].type = targetTypeId;
                        for (let varName of Object.keys(state.constraints[selCIx].values))
                            if (!(varName in targetType.variables))
                                delete state.constraints[selCIx].values[varName];
                        for (let varName of Object.keys(targetType.variables))
                            if (!(varName in state.constraints[selCIx].values))
                                state.constraints[selCIx].values[varName] = constraint.values[varName];
                    }
                updateVisuals({ storage: true, svg: true });
            });
        });

        let uniqueConstraintTypes = [...new Set(state.constraints.map(c => c.type))];
        uniqueConstraintTypes.sort((a, b) => b - a);
        function getConstraintColor(cType, selected)
        {
            return `hsl(${(220 + 360 / uniqueConstraintTypes.length * uniqueConstraintTypes.indexOf(cType)) % 360}, ${selected ? '80%, 50%' : '80%, 90%'})`;
        }

        // If constraintSelectionUpdated is true, this is done further up in the Blazor callback
        if (!constraintSelectionUpdated)
            updateConstraintAndCellSelection();

        // Decide which buttons to show (“select similar”, “move up/down”, “duplicate”)
        let allSimilar = (selectedConstraints.length > 0 && selectedConstraints.every(ix => state.constraints[ix].type === state.constraints[selectedConstraints[0]].type));
        document.getElementById('constraint-select-similar').style.display = allSimilar ? '' : 'none';
        document.getElementById('constraint-code-section').style.display = allSimilar ? '' : 'none';
        if (allSimilar)
            populateConstraintEditBox(state.constraints[selectedConstraints[0]].type);
        document.getElementById('constraint-dup').style.display = selectedConstraints.length > 0 ? '' : 'none';

        let firstSel = Math.min(...selectedConstraints);
        let lastSel = Math.max(...selectedConstraints) + 1;
        let hasGap = (lastSel - firstSel) !== selectedConstraints.length;
        document.getElementById('constraint-move-up').style.display = selectedConstraints.length > 0 && (hasGap || firstSel > 0) ? '' : 'none';
        document.getElementById('constraint-move-down').style.display = selectedConstraints.length > 0 && (hasGap || lastSel < state.constraints.length) ? '' : 'none';

        Array.from(constraintList.querySelectorAll('.constraint')).forEach(constraintDiv =>
        {
            let cIx = constraintDiv.dataset.index | 0;
            let constraint = state.constraints[cIx];
            constraintDiv.style.backgroundColor = getConstraintColor(constraint.type, selectedConstraints.includes(cIx));
            constraintDiv.style.color = selectedConstraints.includes(cIx) ? 'white' : 'black';
            setClass(constraintDiv, 'custom', constraint.type < 0);
            let cType = getConstraintType(constraint.type);
            constraintDiv.querySelector('.name>span').innerText = cType.name;
            constraintDiv.querySelector('.mini-btn.merge').style.display = selectedConstraints.length > 1 && !allSimilar && selectedConstraints.includes(cIx) ? 'block' : 'none';
            setClass(constraintDiv, 'expanded', constraint.expanded);
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
                    dotNet('GenerateOutline', [btn.dataset.regions, w, h], svg => { document.getElementById('outline-svg').innerHTML = svg; });
                };
                btn.onmouseout = function()
                {
                    document.getElementById('outline-svg').innerHTML = '';
                };
                setButtonHandler(btn, () =>
                {
                    document.getElementById('outline-svg').innerHTML = '';
                    let regions = JSON.parse(btn.dataset.regions);
                    setter(regions);
                    selectedCells = [];
                    selectedConstraints = [regCtrl.dataset.constraintix | 0];
                    lastSelectedConstraint = selectedConstraints[0];
                    editingConstraintType = null;
                    regCtrl.zingaRegions = regions;
                    updateVisuals();
                });
            });
        });

        // Selection arrows
        let selectionArrowsSvg = '';
        for (let selIx = 0; selIx < selectedCells.length - 1; selIx++)
        {
            let x1 = selectedCells[selIx] % w;
            let y1 = (selectedCells[selIx] / w) | 0;
            let x2 = selectedCells[selIx + 1] % w;
            let y2 = (selectedCells[selIx + 1] / w) | 0;
            let angle = Math.atan2(y2 - y1, x2 - x1);
            selectionArrowsSvg += `<path d='M${x1 + .4 * Math.cos(angle)} ${y1 + .4 * Math.sin(angle)} ${x2 - .4 * Math.cos(angle)} ${y2 - .4 * Math.sin(angle)}' transform='translate(${.5 + .1 * Math.cos(angle + Math.PI / 2)}, ${.5 + .1 * Math.sin(angle + Math.PI / 2)})' />`;
        }
        document.getElementById('selection-arrows-svg').innerHTML = selectionArrowsSvg;

        // Last selected constraint
        let lscTh = document.getElementById('add-last-selected-constraint');
        if (lastSelectedConstraint === null)
            lscTh.style.display = 'none';
        else
        {
            lscTh.style.display = 'table-row';
            lscTh.querySelector('td').innerText = `Last selected constraint (${getConstraintType(state.constraints[lastSelectedConstraint].type).name})`;
        }

        function fixViewBox()
        {
            let puzzleSvg = puzzleDiv.querySelector('svg.puzzle-svg');

            // — move the button row so that it’s below the puzzle
            let buttons = document.getElementById('bb-buttons');
            let sudokuBBox = document.getElementById('bb-puzzle').getBBox({ fill: true, stroke: true, markers: true, clipped: true });
            buttons.setAttribute('transform', `translate(0, ${Math.max(h, sudokuBBox.y + sudokuBBox.height) + .5})`);

            // — move the global constraints so they’re to the left of the puzzle
            let globalBox = document.getElementById('constraint-svg-global');
            globalBox.setAttribute('transform', `translate(${sudokuBBox.x - 1.5}, 0)`);

            // — change the viewBox so that it includes everything
            let fullBBox = document.getElementById('bb-everything').getBBox();
            puzzleSvg.setAttribute('viewBox', `${fullBBox.x - .1} ${fullBBox.y - .1} ${fullBBox.width + .2} ${fullBBox.height + .5}`);
            let selectionFilter = document.getElementById('constraint-selection-shadow');
            selectionFilter.setAttribute('x', fullBBox.x - .1);
            selectionFilter.setAttribute('y', fullBBox.y - .1);
            selectionFilter.setAttribute('width', fullBBox.width + .2);
            selectionFilter.setAttribute('height', fullBBox.height + .5);
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
            document.getElementById('puzzle-width-input').value = state.width;
            document.getElementById('puzzle-height-input').value = state.height;

            if (!Array.isArray(state.links))
                state.links = [];
            document.getElementById('links').innerHTML = `
                ${state.links.length > 0 ? '<thead><tr><th></th><th>Text</th><th>URL</th></tr></thead>' : ''}
                <tbody>
                    ${state.links.map(_ => `
                        <tr class='link'>
                            <td><button class='mini-btn remove'></button></td>
                            <td><input type='text' class='text' /></td>
                            <td><input type='text' class='url' /></td>
                        </tr>
                    `).join('')}
                </tbody>
            `;
            let ts = Array.from(document.querySelectorAll('#links .text'));
            let us = Array.from(document.querySelectorAll('#links .url'));
            let delBtns = Array.from(document.querySelectorAll('#links .remove'));
            state.links.forEach((lnk, ix) =>
            {
                ts[ix].value = lnk.text;
                ts[ix].onchange = function() { saveUndo(); lnk.text = ts[ix].value; updateVisuals({ storage: true, metadata: true }); };
                us[ix].value = lnk.url;
                us[ix].onchange = function() { saveUndo(); lnk.url = us[ix].value; updateVisuals({ storage: true, metadata: true }); };
                setButtonHandler(delBtns[ix], () => { saveUndo(); state.links.splice(ix, 1); updateVisuals({ storage: true, metadata: true }); });
            });
        }

        if (lastFocusedElement)
        {
            let elem = document.getElementById(lastFocusedElement);
            if (elem)
                elem.focus();
        }
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
    let blazorQueue = [];

    // Variables: UI elements
    let puzzleDiv = document.getElementById('puzzle');
    let puzzleContainer = document.getElementById('puzzle-container');
    let sidebarDiv = document.getElementById('sidebar');
    let constraintList = document.getElementById('constraint-list');
    let constraintCodeBox = document.getElementById('constraint-code-section');
    let constraintCodeExpander = constraintCodeBox.querySelector('.expand');

    // Variables: UI other
    let draggingMode = null;
    let selectedCells = [];
    let lastSelectedCell = 0;
    let selectedConstraints = [];
    let lastSelectedConstraint = null;
    let editingConstraintType = null;
    let editingConstraintTypeParameter = null;
    let lastCellLineDir = null;
    let lastCellLineCell = null;
    let lastFocusedElement = null;
    let constraintResults = [];
    let constraintResultsSel = null;

    // Variables: state, editing, constraints
    let constraintTypes = JSON.parse(puzzleDiv.dataset.constrainttypes || null) || {};
    let state = makeEmptyState();
    let undoBuffer = [];
    let redoBuffer = [];
    let constraintErrors = [];

    // Events
    puzzleContainer.onmousedown = function(ev)
    {
        if (!ev.shiftKey && !ev.ctrlKey)
        {
            pressEscape();
            remoteLog2(`onmousedown puzzleContainer`);
        }
        else
            remoteLog2(`onmousedown puzzleContainer (canceled)`);
    };
    puzzleContainer.onmouseup = handler(puzzleContainer.ontouchend = function(ev)
    {
        if (ev.type !== 'touchend' || ev.touches.length === 0)
            draggingMode = null;
        remoteLog(`${ev.type} puzzleContainer`);
    });
    document.body.addEventListener('focusin', function(ev) { if (ev.target.id) lastFocusedElement = ev.target.id; });

    setCellSelectionEvents();
    Array.from(document.querySelectorAll('#sidebar>.tabs>.tab')).forEach(tab => setButtonHandler(tab, function() { selectTab(tab.dataset.tab); }));

    setButtonHandler(document.getElementById('puzzle-test'), () => { window.open(`${window.location.protocol}//${window.location.host}/test`); });
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
    setButtonHandler(document.getElementById('constraint-select-similar'), () =>
    {
        selectedConstraints = state.constraints.map((cstr, cIx) => cstr.type === state.constraints[selectedConstraints[0]].type ? cIx : null).filter(c => c !== null);
        editingConstraintType = null;
        updateVisuals();
        sidebarDiv.focus();
    });
    setButtonHandler(document.getElementById('constraint-dup'), duplicateConstraints);
    setButtonHandler(document.getElementById('constraint-move-up'), () => moveConstraints(true));
    setButtonHandler(document.getElementById('constraint-move-down'), () => moveConstraints(false));
    setButtonHandler(document.getElementById('constraint-search-cancel'), () => { document.getElementById('constraint-search').classList.remove('shown'); puzzleContainer.focus(); });
    setButtonHandler(constraintCodeExpander, function() { setClass(constraintCodeBox, 'expanded', !constraintCodeBox.classList.contains('expanded')); });
    setButtonHandler(constraintCodeBox.querySelector('.label'), function() { });

    document.getElementById('puzzle-title-input').onchange = function() { saveUndo(); state.title = document.getElementById('puzzle-title-input').value; updateVisuals({ storage: true }); };
    document.getElementById('puzzle-author-input').onchange = function() { saveUndo(); state.author = document.getElementById('puzzle-author-input').value; updateVisuals({ storage: true }); };
    document.getElementById('puzzle-rules-input').onchange = function() { saveUndo(); state.rules = document.getElementById('puzzle-rules-input').value; updateVisuals({ storage: true }); };
    Array.from(document.querySelectorAll('#puzzle-width-input,#puzzle-height-input')).forEach(elem =>
    {
        elem.onchange = function()
        {
            let oldW = state.width, oldH = state.height;
            let newW = document.getElementById('puzzle-width-input').value | 0;
            let newH = document.getElementById('puzzle-height-input').value | 0;
            if (newW < 1)
            {
                document.getElementById('puzzle-width-input').value = 1;
                return;
            }
            if (newH < 1)
            {
                document.getElementById('puzzle-height-input').value = 1;
                return;
            }
            saveUndo();
            let newGivens = Array(newW * newH).fill(null);
            for (let y = 0; y < oldH && y < newH; y++)
                for (let x = 0; x < oldW && x < newW; x++)
                    newGivens[x + newW * y] = state.givens[x + oldW * y];
            state.width = newW;
            state.height = newH;
            state.givens = newGivens;
            function translateValue(value, type)
            {
                if (type === 'cell')
                {
                    let x = value % oldW;
                    let y = (value / oldW) | 0;
                    return (x < newW && y < newH) ? x + newW * y : null;
                }
                let res = /^list\((.*)\)$/.exec(type);
                return res ? value.map(innerVal => translateValue(innerVal, res[1])).filter(v => v !== null || res[1] !== 'cell') : value;
            }
            state.regions = state.regions.map(rg => rg.map(c => translateValue(c, 'cell')).filter(c => c !== null)).filter(rg => rg.length > 0);
            for (let i = 0; i < state.constraints.length; i++)
            {
                let t = getConstraintType(state.constraints[i].type);
                for (let v of Object.keys(t.variables))
                    state.constraints[i].values[v] = translateValue(state.constraints[i].values[v], t.variables[v]);
                let [spVar, spType] = getSpecialVariable(t.kind);
                if (spVar !== null)
                {
                    let enf = enforceConstraintKind(t.kind, state.constraints[i].values[spVar]);
                    if (enf.valid)
                        state.constraints[i].values[spVar] = enf.value;
                    else
                    {
                        console.log(spVar, t, state.constraints[i], enf);
                        state.constraints.splice(i--, 1);
                        continue;
                    }
                }
            }
            updateVisuals({ storage: true, svg: true, metadata: true });
        };
    });
    setButtonHandler(document.getElementById('region-add'), () =>
    {
        if (selectedCells.length < 1)
        {
            alert('Select a set of cells to add a region.');
            return;
        }
        let cells = [...new Set(selectedCells)];
        cells.sort((c1, c2) => c1 - c2);
        if (state.regions.findIndex(rg => cells.length === rg.length && cells.every((c, ix) => c === rg[ix])) !== -1)
            return;
        saveUndo();
        state.regions.push(cells);
        for (let rg = state.regions.length - 2; rg >= 0; rg--)
        {
            state.regions[rg] = state.regions[rg].filter(cell => !cells.includes(cell));
            if (state.regions[rg].length === 0)
                state.regions.splice(rg, 1);
        }
        state.regions.sort((r1, r2) => r1[0] - r2[0]);
        updateVisuals({ svg: true, storage: true });
    });
    setButtonHandler(document.getElementById('region-remove-all'), () =>
    {
        if (state.regions.length > 0)
        {
            saveUndo();
            state.regions = [];
            updateVisuals({ svg: true, storage: true });
        }
    });
    setButtonHandler(document.getElementById('region-fill'), () =>
    {
        let w = state.width, h = state.height, unusedCells = ns(w * h).map(c => c).filter(c => !state.regions.some(r => r.includes(c)));
        if (unusedCells.length < 1)
            return;
        saveUndo();
        state.regions.push(unusedCells);
        state.regions.sort((r1, r2) => r1[0] - r2[0]);
        selectCells(unusedCells);
        updateVisuals({ svg: true, storage: true });
    });
    setButtonHandler(document.getElementById('link-add'), () =>
    {
        saveUndo();
        if (!Array.isArray(state.links))
            state.links = [];
        state.links.push({ text: '', url: '' });
        updateVisuals({ storage: true, metadata: true });
        let ts = Array.from(document.querySelectorAll('#links .text'));
        ts[ts.length - 1].focus();
    });
    setButtonHandler(document.getElementById('value-add'), () =>
    {
        alert('Not implemented.');
    });

    constraintCodeBox.querySelector('.label').ondblclick = function(ev) { if (ev.target !== constraintCodeExpander) setClass(constraintCodeBox, 'expanded', !constraintCodeBox.classList.contains('expanded')); };

    setConstraintCodeEditingEvent('name', (el, ev) => { el.onkeyup = ev; }, cTypeId => getConstraintType(cTypeId).name, (cTypeId, v) => { getConstraintType(cTypeId).name = v; });
    setConstraintCodeEditingEvent('logic', (el, ev) => { el.onkeyup = ev; }, cTypeId => getConstraintType(cTypeId).logic, (cTypeId, v) => { getConstraintType(cTypeId).logic = v; });
    setConstraintCodeEditingEvent('svg', (el, ev) => { el.onkeyup = ev; }, cTypeId => getConstraintType(cTypeId).svg, (cTypeId, v) => { getConstraintType(cTypeId).svg = v; });
    setConstraintCodeEditingEvent('svgdefs', (el, ev) => { el.onkeyup = ev; }, cTypeId => getConstraintType(cTypeId).svgdefs, (cTypeId, v) => { getConstraintType(cTypeId).svgdefs = v; });
    setConstraintCodeEditingEvent('kind', (el, ev) => { el.onchange = ev; }, cTypeId => getConstraintType(cTypeId).kind, (cTypeId, v) =>
    {
        let cType = getConstraintType(cTypeId);
        cType.kind = v;
        let [spVar, spType] = getSpecialVariable(v);
        if (spVar !== null)
            cType.variables[spVar] = spType;
        populateConstraintEditBox(cTypeId);
    });

    puzzleContainer.addEventListener('keyup', ev =>
    {
        if (ev.key === 'Control')
        {
            selectedCells = [...new Set(selectedCells)];
            document.getElementById('selection-arrows-svg').style.opacity = 0;
        }
    });
    puzzleContainer.addEventListener('keydown', ev =>
    {
        let str = keyName(ev), anyFunction = true, w = state.width, h = state.height;

        function ArrowMovement(dx, dy, mode)
        {
            let toSelect = lastSelectedCell;
            if (selectedCells.length !== 0)
            {
                let lastCell = lastCellLineCell !== null ? lastCellLineCell : selectedCells[selectedCells.length - 1];
                let newX = ((lastCell % w) + w + dx) % w;
                let newY = (((lastCell / w) | 0) + h + dy) % h;
                toSelect = newX + w * newY;
            }
            selectCell(toSelect, mode);
            updateVisuals();
        }

        switch (str)
        {
            // Keys that change something
            case 'Digit0': case 'Numpad0':
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
            case 'Backspace':
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
                let shortcutLetter = str.substr(str.length - 1).toLowerCase();
                let shortcutConstraint = Object.keys(constraintTypes).filter(id => constraintTypes[id].shortcut === shortcutLetter);
                if (shortcutConstraint.length !== 0)
                    createConstraint(shortcutConstraint[0] | 0);
                break;

            case 'Equal':
                if (lastSelectedConstraint !== null)
                    createConstraint(state.constraints[lastSelectedConstraint].type);
                break;

            case 'Shift+KeyA':
            case 'Shift+KeyB':
            case 'Shift+KeyC':
            case 'Shift+KeyD':
            case 'Shift+KeyE':
            case 'Shift+KeyF':
            case 'Shift+KeyG':
            case 'Shift+KeyH':
            case 'Shift+KeyI':
            case 'Shift+KeyJ':
            case 'Shift+KeyK':
            case 'Shift+KeyL':
            case 'Shift+KeyM':
            case 'Shift+KeyN':
            case 'Shift+KeyO':
            case 'Shift+KeyP':
            case 'Shift+KeyQ':
            case 'Shift+KeyR':
            case 'Shift+KeyS':
            case 'Shift+KeyT':
            case 'Shift+KeyU':
            case 'Shift+KeyV':
            case 'Shift+KeyW':
            case 'Shift+KeyX':
            case 'Shift+KeyY':
            case 'Shift+KeyZ':
                document.getElementById('constraint-search').classList.add('shown');
                let si = document.getElementById('constraint-search-input');
                si.value = str.substr(str.length - 1);
                si.setSelectionRange(1, 1);
                si.focus();
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

            case 'Ctrl+KeyA': selectedCells = ns(w * h).map(c => c); selectedConstraints = []; editingConstraintType = null; updateVisuals(); break;
            case 'Escape': pressEscape(); break;

            case 'Ctrl+ControlLeft':
            case 'Ctrl+ControlRight':
                document.getElementById('selection-arrows-svg').style.opacity = 1;
                break;

            // Undo/redo
            case 'Alt+Backspace':
            case 'Ctrl+KeyZ':
                undo();
                break;

            case 'Alt+Shift+Backspace':
            case 'Ctrl+Shift+KeyZ':
            case 'Ctrl+KeyY':
                redo();
                break;

            // Debug
            case 'Ctrl+KeyO':
                console.log(selectedCells.join(", "));
                break;
            case 'Ctrl+Shift+KeyO':
                console.log(state);
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
    sidebarDiv.addEventListener('keydown', ev =>
    {
        if (ev.target !== sidebarDiv)
            return true;

        let str = keyName(ev);
        let anyFunction = true;

        switch (str)
        {
            // Keys that change something
            case 'Delete': case 'Backspace': clearCells(); break;
            case 'Ctrl+KeyD': duplicateConstraints(); break;

            case 'Alt+ArrowUp': moveConstraints(true); break;
            case 'Alt+ArrowDown': moveConstraints(false); break;

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

            case 'Escape': pressEscape(); break;
            case 'Ctrl+KeyA': selectedCells = []; selectedConstraints = state.constraints.map((_, c) => c); editingConstraintType = null; updateVisuals(); break;

            // Undo/redo
            case 'Alt+Backspace':
            case 'Ctrl+KeyZ':
                undo();
                break;

            case 'Alt+Shift+Backspace':
            case 'Ctrl+Shift+KeyZ':
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
    document.getElementById('constraint-search-input').addEventListener('keydown', ev =>
    {
        let str = keyName(ev);
        let anyFunction = true;

        switch (str)
        {
            case 'Escape': pressEscape(); break;
            case 'Tab':
                if (constraintResults.length === 0)
                    break;
                constraintResultsSel = ((constraintResultsSel ?? -1) + 1) % constraintResults.length;
                constraintResults.forEach((cri, ix) => { if (ix === constraintResultsSel) cri.div.classList.add('kbsel'); else cri.div.classList.remove('kbsel'); });
                break;
            case 'Enter':
                if (constraintResultsSel === null || constraintResultsSel < 0 || constraintResultsSel >= constraintResults.length)
                    break;
                addConstraintFromSearch(constraintResults[constraintResultsSel].constraint, constraintResults[constraintResultsSel].cType);
                document.getElementById('constraint-search').classList.remove('shown');
                puzzleContainer.focus();
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
    document.getElementById('constraint-search-input').addEventListener('keyup', ev =>
    {
        let isLetter = ev.key.length === 1 && ((ev.key.charCodeAt(0) >= 0x41 && ev.key.charCodeAt(0) <= 0x5A) || (ev.key.charCodeAt(0) >= 0x61 && ev.key.charCodeAt(0) <= 0x7A));
        if (isLetter || ev.key === ' ' || ev.key === 'Backspace')
        {
            let val = document.getElementById('constraint-search-input').value;
            if (val.length > 0)
                runConstraintSearch(val);
        }
    });

    //document.addEventListener('paste', ev =>
    //{
    //    let newState;
    //    try { newState = JSON.parse(ev.clipboardData.getData('text')); }
    //    catch { return; }
    //    saveUndo();
    //    state = newState;
    //    updateVisuals({ storage: true, svg: true, metadata: true });
    //});


    /// — RUN

    // Blazor
    Blazor.start({}).then(() =>
    {
        for (let i = 0; i < blazorQueue.length; i++)
            DotNet.invokeMethodAsync('ZingaWasm', blazorQueue[i][0], ...blazorQueue[i][1]).then(blazorQueue[i][2]);
        blazorQueue = null;
    });

    // Retrieve state from localStorage
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
            if (state.rules === undefined || state.rules === null) state.rules = '';
            if (state.customConstraintTypes === undefined || state.customConstraintTypes === null) state.customConstraintTypes = [];
            if (state.width === undefined || state.width === null || state.height === undefined || state.height === null || state.width <= 0 || state.height <= 0)
            {
                state.width = 9;
                state.height = 9;
            }

            if (!Array.isArray(state.givens) || state.givens.length != state.width * state.height)
                state.givens = Array(state.width * state.height).fill(null);
            state.constraints = Array.isArray(state.constraints) ? state.constraints.filter(c => 'type' in c && Number.isInteger(c.type)) : [];
            for (let i = 0; i < state.constraints.length; i++)
            {
                let cType = getConstraintType(state.constraints[i].type);
                for (let varName of Object.keys(state.constraints[i].values))
                    if (!(varName in cType.variables))
                        delete state.constraints[i].values[varName];
                for (let varName of Object.keys(cType.variables))
                    state.constraints[i].values[varName] = coerceValue(state.constraints[i].values[varName], cType.variables[varName]);
            }
            upgrade(state);
        }

        let undoB = localStorage.getItem(`zinga-edit-undo`);
        let redoB = localStorage.getItem(`zinga-edit-redo`);

        undoBuffer = undoB ? JSON.parse(undoB) : [];
        redoBuffer = redoB ? JSON.parse(redoB) : [];
    }
    catch { }

    // UI
    updateVisuals({ storage: true, svg: true, metadata: true });
    selectTab('puzzle');
    puzzleContainer.focus();
});
