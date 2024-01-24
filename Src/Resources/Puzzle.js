window.onload = (async function()
{
	/// — FUNCTIONS

	// Utility
	async function dotNet(method, args) { return DotNet.invokeMethodAsync('ZingaWasm', method, ...args); }
	function fixViewBox()
	{
		// ##  Resize the buttons in the button rows below the puzzle grid
		let buttonRows = [
			{ row: 0, btnIds: values.map(v => `btn-${v}`) },
			{ row: 0, btnIds: ns(9).map(c => `btn-color-${c}`) },
			{ row: 1, btnIds: ['btn-normal', 'btn-corner', 'btn-center', 'btn-color'] },
			{ row: 2, btnIds: ['btn-clear', 'btn-undo', 'btn-redo', 'btn-sidebar'] }
		];
		let btnMargin = .135, btnPadding = .135, scaler = document.getElementById('bb-buttons-scaler');
		scaler.removeAttribute('transform');

		// Calculate the width of each row assuming each button is unscaled and at its minimum possible width that still accommodates its text
		let rowWidths = [], boundingBoxes = [];
		for (let rowIx = 0; rowIx < buttonRows.length; rowIx++)
		{
			let totalWidth = 0, bbs = [];
			for (let btnId of buttonRows[rowIx].btnIds)
			{
				let label = document.querySelector(`#${btnId}>g.label`);
				label.removeAttribute('transform');
				let bb = label.getBBox({ fill: true, stroke: true, markers: true, clipped: true });
				bbs.push(bb);
				totalWidth += bb.width + 2 * btnPadding;
			}
			rowWidths.push(totalWidth + (buttonRows[rowIx].btnIds.length - 1) * btnMargin);
			boundingBoxes.push(bbs);
		}
		let targetWidth = Math.max(...rowWidths, width);

		// Add padding to all the buttons so that every row attains the width of ‘targetWidth’
		for (let rowIx = 0; rowIx < buttonRows.length; rowIx++)
		{
			let extraPadding = (targetWidth - rowWidths[rowIx]) / buttonRows[rowIx].btnIds.length;
			let x = 0;
			for (let btnIx = 0; btnIx < buttonRows[rowIx].btnIds.length; btnIx++)
			{
				let btnId = buttonRows[rowIx].btnIds[btnIx];
				let rect = document.querySelector(`#${btnId}>rect.clickable`);
				let label = document.querySelector(`#${btnId}>g.label`);
				let w = boundingBoxes[rowIx][btnIx].width + 2 * btnPadding + extraPadding;
				rect.setAttribute('x', x);
				rect.setAttribute('y', buttonRows[rowIx].row);
				rect.setAttribute('width', w);
				label.setAttribute('transform', `translate(${x + w / 2} ${buttonRows[rowIx].row})`);
				x += w + btnMargin;
			}
		}
		if (targetWidth > width)
			scaler.setAttribute('transform', `scale(${width / targetWidth})`);

		// Move the button row so that it’s below the puzzle
		let buttons = document.getElementById('bb-buttons');
		let extraBBox = document.getElementById('bb-puzzle').getBBox({ fill: true, stroke: true, markers: true, clipped: true });
		buttons.setAttribute('transform', `translate(0, ${Math.max(height + .4, extraBBox.y + extraBBox.height + .25)})`);

		// Move the global constraints so they’re to the left of the puzzle
		let globalBox = document.getElementById('constraint-svg-global');
		globalBox.setAttribute('transform', `translate(${extraBBox.x - 1.5}, 0)`);

		// Change the viewBox so that it includes everything
		let fullBBox = document.getElementById('bb-everything').getBBox({ fill: true, stroke: true, markers: true, clipped: true });
		let left = Math.min(-.4, fullBBox.x - .1);
		let top = Math.min(-.4, fullBBox.y - .1);
		let right = Math.max(width + .4, fullBBox.x + fullBBox.width + .2);
		let bottom = Math.max(height + .4, fullBBox.y + fullBBox.height + .2);
		document.getElementById('puzzle-svg').setAttribute('viewBox', `${left} ${top} ${right - left} ${bottom - top}`);
		let conflictFilter = document.getElementById('constraint-invalid-shadow');
		conflictFilter.setAttribute('x', left);
		conflictFilter.setAttribute('y', top);
		conflictFilter.setAttribute('width', right - left);
		conflictFilter.setAttribute('height', bottom - top);
	}
	function getLsKey() { return puzzleId !== 'test' || (width === 9 && height === 9 && values.length === 9 && values.every((v, ix) => v === ix + 1)) ? '' : `-${width}-${height}-${values.join('-')}`; }
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
	function ns(num) { return Array.from({ length: num }, (_, ix) => ix); }
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
	async function checkSudokuValid()
	{
		let grid = ns(width * height).map(c => getDisplayedSudokuDigit(state, c));
		Array.from(document.querySelectorAll('.region-invalid')).forEach(elem => { elem.setAttribute('opacity', 0); });
		let isValid = true;

		// Check the Sudoku rules (rows, columns and regions)
		if (rowsUnique)
			for (let r = 0; r < height; r++)
				for (let colA = 0, go = true; colA < width && go; colA++)
					if (grid[colA + width * r] !== null)
						for (let colB = colA + 1; colB < width && go; colB++)
							if (grid[colA + width * r] === grid[colB + width * r])
							{
								if (showErrors)
									document.getElementById(`row-invalid-${r}`).setAttribute('opacity', 1);
								isValid = false;
								go = false;
							}
		if (columnsUnique)
			for (let c = 0; c < width; c++)
				for (let rowA = 0, go = true; rowA < height && go; rowA++)
					if (grid[c + width * rowA] !== null)
						for (let rowB = rowA + 1; rowB < height && go; rowB++)
							if (grid[c + width * rowA] === grid[c + width * rowB])
							{
								if (showErrors)
									document.getElementById(`column-invalid-${c}`).setAttribute('opacity', 1);
								isValid = false;
								go = false;
							}
		for (let rgIx = 0, region = regions[rgIx]; rgIx < regions.length && (region = regions[rgIx]); rgIx++)
			for (let cellA = 0, go = true; cellA < region.length && go; cellA++)
				if (grid[region[cellA]] !== null)
					for (let cellB = cellA + 1; cellB < region.length && go; cellB++)
						if (grid[region[cellA]] === grid[region[cellB]])
						{
							if (showErrors)
								document.getElementById(`region-invalid-${rgIx}`).setAttribute('opacity', 1);
							isValid = false;
							go = false;
						}

		// Check that all cells in the Sudoku grid have a digit
		if (isValid && grid.some(c => c === null))
			isValid = null;

		setClass(puzzleDiv, 'solved', false);

		// Check if any constraints are violated
		if (showErrors || grid.every(d => d !== null))
		{
			let violatedConstraintIxs = JSON.parse(await dotNet('CheckConstraints', [JSON.stringify(grid)]));
			setClass(puzzleDiv, 'solved', isValid === true && violatedConstraintIxs.length === 0);
			for (let cIx = 0; cIx < constraints.length; cIx++)
				setClass(document.getElementById(`constraint-svg-${cIx}`), 'violated', showErrors && violatedConstraintIxs.includes(cIx));
		}
		else
			Array.from(document.querySelectorAll('#constraint-svg>g,#constraint-svg-global>g')).forEach(g => { g.removeAttribute('filter'); g.classList.remove('violated'); });
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
			colors: Array(width * height).fill(null),
			cornerNotation: Array(width * height).fill(null).map(_ => []),
			centerNotation: Array(width * height).fill(null).map(_ => []),
			enteredDigits: Array(width * height).fill(null),
			dimmedConstraints: []
		};

		// Decode Sudoku grid
		for (let cell = width * height - 1; cell >= 0; cell--)
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

			let nv = BigInt(values.length + 2);
			let code = val % nv;
			val = val / nv;
			// Complex case: center notation and corner notation
			if (code === nv - 1n)
			{
				// Center notation
				for (let valIx = values.length - 1; valIx >= 0; valIx--)
				{
					if (val % 2n === 1n)
						st.centerNotation[cell].unshift(values[valIx]);
					val = val / 2n;
				}

				// Corner notation
				for (let valIx = values.length - 1; valIx >= 0; valIx--)
				{
					if (val % 2n === 1n)
						st.cornerNotation[cell].unshift(values[valIx]);
					val = val / 2n;
				}
			}
			else if (code > 0n)
				st.enteredDigits[cell] = values[Number(code) - 1];
		}

		// Dimmed constraints
		for (let i = 0; i < constraints.length; i++)
			if ((val & (1n << BigInt(i))) != 0)
				st.dimmedConstraints.push(i);
		return st;
	}
	function encodeState(st)
	{
		let val = 0n, nv = BigInt(values.length + 2);

		// Dimmed constraints
		for (let constrIx of st.dimmedConstraints)
			val = val + (1n << BigInt(constrIx));

		// Encode the Sudoku grid
		for (let cell = 0; cell < width * height; cell++)
		{
			// Compact representation of an entered digit or a completely empty cell
			if (st.enteredDigits[cell] !== null)
				val = (val * nv) + BigInt(values.indexOf(st.enteredDigits[cell]) + 1);
			else if (st.cornerNotation[cell].length === 0 && st.centerNotation[cell].length === 0)
				val = val * nv;
			else
			{
				// corner notation
				for (let valIx = 0; valIx < values.length; valIx++)
					val = val * 2n + (st.cornerNotation[cell].includes(values[valIx]) ? 1n : 0n);

				// center notation
				for (let valIx = 0; valIx < values.length; valIx++)
					val = val * 2n + (st.centerNotation[cell].includes(values[valIx]) ? 1n : 0n);

				val = val * nv + (nv - 1n);
			}

			// Encode colors
			if (st.colors[cell].length === 0)   // Common case: no color
				val = val * 3n;
			else if (st.colors[cell].length === 1)  // Single color: one of 9
				val = (val * 9n + BigInt(st.colors[cell][0])) * 3n + 1n;
			else    // Multiple colors: bitfield
			{
				for (let color = 0; color < 9; color++)
					val = val * 2n + (st.colors[cell].includes(color) ? 1n : 0n);
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
	function enterCenterNotation(digit) { enterNotation(digit, cell => state.centerNotation[cell]); }
	function enterCornerNotation(digit) { enterNotation(digit, cell => state.cornerNotation[cell]); }
	function enterNotation(digit, getNotation)
	{
		if (selectedCells.every(c => getDisplayedSudokuDigit(state, c)))
			return;
		saveUndo();
		let allHaveDigit = selectedCells.filter(c => !getDisplayedSudokuDigit(state, c)).every(c => getNotation(c).includes(digit));
		selectedCells.forEach((cell, ix) =>
		{
			// ignore cells with a full digit in them, and duplicated entries of ‘selectedCells’
			if (getDisplayedSudokuDigit(state, cell) || selectedCells.indexOf(cell) !== ix)
				return;
			var notation = getNotation(cell);
			if (allHaveDigit)
				notation.splice(notation.indexOf(digit), 1);
			else if (!notation.includes(digit))
			{
				notation.push(digit);
				notation.sort();
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
			colors: ns(width * height).map(_ => []),
			cornerNotation: ns(width * height).map(_ => []),
			centerNotation: ns(width * height).map(_ => []),
			enteredDigits: Array(width * height).fill(null),
			dimmedConstraints: []
		};
	}
	function pressDigit(digit, ev, forceMode)
	{
		if (selectedCells.length === 0)
		{
			// Highlight digits
			if (ev && ev.shiftKey)
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
			switch (forceMode ?? mode)
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
					setCellColor(digit);
					break;
			}
		}
	}
	function retrieveStateFromLocalStorage()
	{
		try
		{
			let lsKey = getLsKey();
			let optB = localStorage.getItem(`zinga-${puzzleId}${lsKey}-opt`);
			let opt = optB && JSON.parse(optB);

			showErrors = opt ? !!opt.showErrors : true;
			multiColorMode = opt ? !!opt.multiColorMode : false;
			sidebarOn = opt ? !!opt.sidebarOn : true;

			let str = localStorage.getItem(`zinga-${puzzleId}${lsKey}`);
			let item = null;
			if (str !== null)
				try { item = decodeState(str); }
				catch { item = null; }
			state = (item && item.cornerNotation && item.centerNotation && item.enteredDigits && item.colors) ? item : makeCleanState();
			state.dimmedConstraints ??= [];

			let undoB = localStorage.getItem(`zinga-${puzzleId}${lsKey}-undo`);
			let redoB = localStorage.getItem(`zinga-${puzzleId}${lsKey}-redo`);

			undoBuffer = undoB ? undoB.split(' ') : [];
			redoBuffer = redoB ? redoB.split(' ') : [];
		}
		catch { }
	}
	function selectCell(cell, mode)
	{
		// Processes a mouse click or keypress that causes a cell selection. The cell may be added to an already existing selection.
		if (mode === 'toggle')
		{
			if (selectedCells.length === 1 && selectedCells[0] === cell)
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
		document.getElementById('btn-clear').classList.remove('warning');
		document.querySelector('#btn-clear>g.label>text').textContent = 'Clear';
	}
	function selectSimilar(ev)
	{
		let cellsToSelect;
		if (mode === 'color')
		{
			let selectedColors = new Set(selectedCells.reduce((p, n) => p.concat(state.colors[n]), []));
			cellsToSelect = ns(width * height).filter(c => state.colors[c].some(col => selectedColors.has(col)));
		}
		else
		{
			let selectedDigits = new Set(selectedCells.reduce((p, n) =>
			{
				let d = getDisplayedSudokuDigit(state, n);
				return p.concat(d === null ? state.cornerNotation[n].concat(state.centerNotation[n]) : [d]);
			}, []));
			cellsToSelect = ns(width * height).filter(c =>
			{
				let d = getDisplayedSudokuDigit(state, c);
				return d === null ? state.cornerNotation[c].concat(state.centerNotation[c]).some(dg => selectedDigits.has(dg)) : selectedDigits.has(d);
			});
		}
		selectedCells = ev.shiftKey ? [...new Set(cellsToSelect.concat(selectedCells))] : cellsToSelect;
		updateVisuals();
	}
	function setDynamicEvents()
	{
		// This function hooks up all of the UI events (onclick etc.) relating to elements that are re-created in updatePuzzleFromEdit().
		Array.from(puzzleDiv.getElementsByClassName('sudoku-cell')).forEach(cellRect =>
		{
			let mostRecentMouseDown = null;
			let cell = parseInt(cellRect.dataset.cell);
			cellRect.onclick = handler(function() { remoteLog2(`onclick ${cell}`); });
			cellRect.onmousedown = cellRect.ontouchstart = handler(function(ev)
			{
				if (mostRecentMouseDown !== null && new Date() - mostRecentMouseDown <= 500)
				{
					selectSimilar(ev);
					mostRecentMouseDown = null;
					return;
				}
				mostRecentMouseDown = +new Date();
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
		ns(values.length).forEach(valIx => { setButtonHandler(document.querySelector(`#btn-${values[valIx]}>rect`), function(ev) { pressDigit(values[valIx], ev); }); });
		ns(9).forEach(color => { setButtonHandler(document.querySelector(`#btn-color-${color}>rect`), function(ev) { pressDigit(color, ev, 'color'); }); });
		setButtonHandler(document.querySelector('#btn-normal>rect'), function() { mode = 'normal'; updateVisuals(); });
		setButtonHandler(document.querySelector('#btn-corner>rect'), function() { mode = 'corner'; updateVisuals(); });
		setButtonHandler(document.querySelector('#btn-center>rect'), function() { mode = 'center'; updateVisuals(); });
		setButtonHandler(document.querySelector('#btn-color>rect'), function() { mode = 'color'; updateVisuals(); });
		setButtonHandler(document.querySelector('#btn-clear>rect'), function()
		{
			let elem = document.getElementById('btn-clear');
			if (!elem.classList.contains('warning'))
			{
				clearCells();
				elem.classList.add('warning');
				puzzleDiv.querySelector(`#btn-clear>g.label>text`).textContent = 'Restart';
			}
			else
			{
				resetClearButton();
				saveUndo();
				state = makeCleanState();
				updateVisuals(true);
			}
		});
		setButtonHandler(document.querySelector('#btn-undo>rect'), undo);
		setButtonHandler(document.querySelector('#btn-redo>rect'), redo);
		setButtonHandler(document.querySelector('#btn-sidebar>rect'), function() { sidebarOn = !sidebarOn; updateVisuals(true); });
		Array.from(document.querySelectorAll('.constraint-svg')).forEach(obj =>
		{
			let idm = /^constraint-svg-(\d+)$/.exec(obj.id);
			if (idm)
			{
				let id = idm[1] | 0;
				obj.addEventListener('click', () =>
				{
					saveUndo();
					let p = state.dimmedConstraints.indexOf(id);
					if (p === -1)
						state.dimmedConstraints.push(id);
					else
						state.dimmedConstraints.splice(p, 1);
					updateVisuals(true);
				});
			}
		});
	}
	async function updatePuzzleFromEdit()
	{
		// This function is only used on the test page
		if (document.hidden || puzzleId !== 'test')
			return;

		let editState = null;
		try { editState = JSON.parse(localStorage.getItem('zinga-edit')); }
		catch { }
		digitsAtPrevCheck = null;
		if (!editState || !editState.givens || !editState.constraints)
			return;

		width = editState.width ?? 9;
		height = editState.height ?? 9;
		rowsUnique = editState.rowsUniq ?? true;
		columnsUnique = editState.colsUniq ?? true;
		values = editState.values ?? [1, 2, 3, 4, 5, 6, 7, 8, 9];
		givens = editState.givens;
		regions = editState.regions;
		constraints = editState.constraints;
		customConstraintTypes = editState.customConstraintTypes;

		document.querySelector('#topbar>.title').innerText = editState.title ?? 'Sudoku';
		document.querySelector('#topbar>.author').innerText = `by ${editState.author ?? 'unknown'}`;
		document.querySelector('#sidebar .links').innerHTML = Array.isArray(editState.links) ? editState.links.map(_ => `<li><a></a></li>`).join('') : '';
		Array.from(document.querySelectorAll('#sidebar .links a')).forEach((obj, ix) => { obj.setAttribute('href', editState.links[ix].url); obj.innerText = editState.links[ix].text; });
		document.title = `Testing: ${editState.title ?? 'Sudoku'} by ${editState.author ?? 'unknown'}`;

		puzzleDiv.dataset.title = editState.title;
		puzzleDiv.dataset.author = editState.author;
		puzzleDiv.dataset.rules = editState.rules;

		let paragraphs = (editState.rules ?? 'Normal Sudoku rules apply: place the digits 1–9 in every row, every column and every 3×3 box.').split(/\r?\n/).filter(s => s !== null && !/^\s*$/.test(s));
		document.getElementById('rules-text').innerHTML = paragraphs.map(_ => '<p></p>').join('');
		Array.from(document.querySelectorAll('#rules-text>p')).forEach((p, pIx) => { p.innerText = paragraphs[pIx]; });

		// Update the puzzle SVG
		document.getElementById('puzzle-svg').innerHTML = await dotNet('RenderPuzzleSvg', [width, height, JSON.stringify(regions), rowsUnique, columnsUnique, JSON.stringify(values), JSON.stringify(constraintTypes), JSON.stringify(customConstraintTypes), JSON.stringify(constraints)]);

		await dotNet('SetupConstraints', [JSON.stringify(constraintTypes), JSON.stringify({ customConstraintTypes: customConstraintTypes, constraints: constraints, givens: givens, width: width, height: height, regions: regions })]);

		retrieveStateFromLocalStorage();
		updateVisuals();
		setDynamicEvents();
		fixViewBox();
		requestAnimationFrame(function() { window.dispatchEvent(new Event('resize')); });
	}
	async function updateVisuals(updateStorage)
	{
		// Update localStorage (only do this when necessary because encodeState() is relatively slow)
		if (localStorage && updateStorage)
		{
			let lsKey = getLsKey();
			localStorage.setItem(`zinga-${puzzleId}${lsKey}`, encodeState(state));
			localStorage.setItem(`zinga-${puzzleId}${lsKey}-undo`, undoBuffer.join(' '));
			localStorage.setItem(`zinga-${puzzleId}${lsKey}-redo`, redoBuffer.join(' '));
			localStorage.setItem(`zinga-${puzzleId}${lsKey}-opt`, JSON.stringify({ showErrors: showErrors, multiColorMode: multiColorMode, sidebarOn: sidebarOn }));
		}
		resetClearButton();

		// Dimmed constraints
		for (let constrIx = 0; constrIx < constraints.length; constrIx++)
			setClass(document.getElementById(`constraint-svg-${constrIx}`), 'dimmed', state.dimmedConstraints.includes(constrIx));

		// Sudoku grid (digits, selection/highlight)
		Array.from(document.querySelectorAll('.cell')).forEach(cellElem =>
		{
			let cell = cellElem.dataset.cell | 0;
			setClass(cellElem, 'highlighted', selectedCells.includes(cell) || highlightedDigits.includes(getDisplayedSudokuDigit(state, cell)));
			for (let color = 0; color < 9; color++)
				setClass(cellElem, `c${color}`, state.colors[cell].length >= 1 && state.colors[cell][0] === color);
		});
		let digitCounts = Array(width * height).fill(0);
		for (let cell = 0; cell < width * height; cell++)
		{
			let digit = getDisplayedSudokuDigit(state, cell);
			let valIx = digit === null ? null : values.indexOf(digit);
			if (valIx !== null)
				digitCounts[valIx]++;

			let intendedText = null;
			let intendedCenterDigits = null;
			let intendedCornerDigits = null;

			if (digit !== null)
				intendedText = digit;
			else
			{
				intendedCenterDigits = state.centerNotation[cell].join('');
				intendedCornerDigits = state.cornerNotation[cell];
			}

			document.getElementById(`sudoku-text-${cell}`).textContent = intendedText !== null ? intendedText : '';
			for (let i = 0; i < 8; i++)
				document.getElementById(`sudoku-corner-text-${cell}-${i}`).textContent = intendedCornerDigits !== null && i < intendedCornerDigits.length ? intendedCornerDigits[i] : '';

			var centerText = document.getElementById(`sudoku-center-text-${cell}`);
			centerText.removeAttribute('transform');
			centerText.textContent = intendedCenterDigits !== null ? intendedCenterDigits : '';
			var ctBb = centerText.getBBox({ fill: true, stroke: true, markers: true, clipped: true });
			centerText.setAttribute('transform', `translate(${cell % width + .5} ${((cell / width) | 0) + .5})${ctBb.width > .8 ? ` scale(${.8 / ctBb.width})` : ''} translate(0, .12)`);

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
		let pretendMode = ctrlPressed ? shiftPressed ? 'color' : 'center' : shiftPressed ? 'corner' : mode;
		setClass(puzzleDiv, 'show-colors', pretendMode === 'color');
		setClass(puzzleContainer, 'dimmable', ctrlPressed && shiftPressed);

		for (let md of ["normal", "center", "corner", "color"])
		{
			setClass(document.getElementById(`btn-${md}`), 'selected', pretendMode === md);
			setClass(puzzleDiv, `mode-${md}`, pretendMode === md);
		}

		for (let valIx = 0; valIx < values.length; valIx++)
		{
			setClass(document.getElementById(`btn-${values[valIx]}`), 'selected', highlightedDigits.includes(values[valIx]));
			setClass(document.getElementById(`btn-${values[valIx]}`), 'success', digitCounts[valIx] === values.length);
		}

		setClass(puzzleDiv, 'sidebar-off', !sidebarOn);
		document.querySelector('#btn-sidebar>g.label>text').textContent = sidebarOn ? 'Less' : 'More';
		document.getElementById('opt-show-errors').checked = showErrors;
		document.getElementById('opt-multi-color').checked = multiColorMode;

		if (digitsAtPrevCheck === null || ns(width * height).some(ix => state.enteredDigits[ix] !== digitsAtPrevCheck[ix]))
		{
			// Check if there are any conflicts (red glow) and/or the puzzle is solved
			await checkSudokuValid();
			digitsAtPrevCheck = Array.from(state.enteredDigits);
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
	await Blazor.start({});

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
	let digitsAtPrevCheck = null;
	let shiftPressed = false;
	let ctrlPressed = false;

	// Variables: puzzle
	let puzzleId = puzzleDiv.dataset.puzzleid || 'unknown';
	let constraintTypes = JSON.parse(puzzleDiv.dataset.constrainttypes);
	let customConstraintTypes = [];
	let constraints = JSON.parse(puzzleDiv.dataset.constraints ?? null) ?? [];
	let width = (puzzleDiv.dataset.width ?? 9) | 0;
	let height = (puzzleDiv.dataset.height ?? 9) | 0;
	let rowsUnique = (puzzleDiv.dataset.rowsuniq ?? "True") === "True";
	let columnsUnique = (puzzleDiv.dataset.colsuniq ?? "True") === "True";
	let regions = JSON.parse(puzzleDiv.dataset.regions ?? null) ?? [[0, 1, 2, 9, 10, 11, 18, 19, 20], [3, 4, 5, 12, 13, 14, 21, 22, 23], [6, 7, 8, 15, 16, 17, 24, 25, 26], [27, 28, 29, 36, 37, 38, 45, 46, 47], [30, 31, 32, 39, 40, 41, 48, 49, 50], [33, 34, 35, 42, 43, 44, 51, 52, 53], [54, 55, 56, 63, 64, 65, 72, 73, 74], [57, 58, 59, 66, 67, 68, 75, 76, 77], [60, 61, 62, 69, 70, 71, 78, 79, 80]];
	let values = JSON.parse(puzzleDiv.dataset.values ?? null) ?? [1, 2, 3, 4, 5, 6, 7, 8, 9];
	let givens = Array(width * height).fill(null);
	for (let givenInf of JSON.parse(puzzleDiv.dataset.givens ?? null) ?? [])
		if ((givenInf[0] | 0) >= 0 && (givenInf[0] | 0) < width * height && values.includes(givenInf[1] | 0))
			givens[givenInf[0] | 0] = givenInf[1] | 0;
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
	document.getElementById('opt-show-errors').onchange = function() { showErrors = !showErrors; updateVisuals(true); };
	document.getElementById('opt-multi-color').onchange = function() { multiColorMode = !multiColorMode; updateVisuals(true); };
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

			let links = [];
			try { links = JSON.parse(puzzleDiv.dataset.links); }
			catch { }

			let newEditState = {
				title: puzzleDiv.dataset.title,
				author: puzzleDiv.dataset.author,
				rules: puzzleDiv.dataset.rules,
				links: links ?? [],
				givens: givens,
				width: width,
				height: height,
				regions: regions,
				rowsUniq: rowsUnique,
				colsUniq: columnsUnique,
				values: values,
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
		let bBox = document.getElementById('bb-puzzle').getBBox({ fill: true, stroke: true, markers: true, clipped: true });
		let margin = .1;
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
		let svgCode = encodeURIComponent(document.getElementById('puzzle-svg').outerHTML
			.replace(/<svg([^>]*)>/, (_, p) => `<svg width="${canvas.width}" height="${canvas.height}"${p.replace(/viewBox=".*?"/, `viewBox='${nBox.x} ${nBox.y} ${nBox.width} ${nBox.height}'`)}><style>${Array.from(document.styleSheets).reduce((p, ss) => p.concat(Array.from(ss.cssRules)), [])
				.filter(rule => !(rule instanceof CSSStyleRule) || rule.selectorText.startsWith('svg#puzzle-svg ')).map(rule => rule.cssText.replace(/^\s*svg#puzzle-svg\s/, '')).join("\n")}</style>`));
		let base64 = "", b64ch = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
		for (let i = 0; i < svgCode.length;)
		{
			let byte1 = svgCode[i] == '%' ? parseInt(svgCode.substring(i += 1, i += 2), 16) : svgCode.charCodeAt(i++);
			let byte2 = i < svgCode.length ? (svgCode[i] == '%' ? parseInt(svgCode.substring(i += 1, i += 2), 16) : svgCode.charCodeAt(i++)) : 32;
			let byte3 = i < svgCode.length ? (svgCode[i] == '%' ? parseInt(svgCode.substring(i += 1, i += 2), 16) : svgCode.charCodeAt(i++)) : 32;
			base64 += b64ch[byte1 >> 2];
			base64 += b64ch[((byte1 & 0x3) << 4) | (byte2 >> 4)];
			base64 += b64ch[((byte2 & 0xF) << 2) | (byte3 >> 6)];
			base64 += b64ch[byte3 & 0x3F];
		}
		img.src = 'data:image/svg+xml;base64,' + base64;
	});
	puzzleContainer.addEventListener('keydown', ev =>
	{
		if (ev.code === 'ControlLeft' || ev.code === 'ControlRight')
		{
			ctrlPressed = true;
			updateVisuals();
		}
		if (ev.code === 'ShiftLeft' || ev.code === 'ShiftRight')
		{
			shiftPressed = true;
			updateVisuals();
		}

		let keyCombo = ev.code;
		if (ev.shiftKey)
			keyCombo = `Shift+${keyCombo}`;
		if (ev.altKey)
			keyCombo = `Alt+${keyCombo}`;
		if (ev.ctrlKey)
			keyCombo = `Ctrl+${keyCombo}`;

		let anyFunction = true;

		function ArrowMovement(dx, dy, mode)
		{
			highlightedDigits = [];
			if (selectedCells.length === 0)
				selectedCells = [0];
			else
			{
				let lastCell = selectedCells[selectedCells.length - 1];
				let newX = ((lastCell % width) + width + dx) % width;
				let newY = (((lastCell / width) | 0) + height + dy) % height;
				selectCell(newX + width * newY, mode);
			}
			updateVisuals();
		}

		switch (keyCombo)
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
			case 'Digit0': case 'Numpad0':
				let pressedDigit = parseInt(keyCombo.substring(keyCombo.length - 1));
				if (mode === 'color' && pressedDigit > 0)
					pressDigit(pressedDigit - 1, ev);
				else if (mode !== 'color')
					pressDigit(pressedDigit, ev);
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
			case 'Ctrl+Digit0': case 'Ctrl+Numpad0':
				enterCenterNotation(parseInt(keyCombo.substring(keyCombo.length - 1)));
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
				//case 'Ctrl+Shift+Digit0': case 'Ctrl+Shift+Numpad0':
				setCellColor(parseInt(keyCombo.substring(keyCombo.length - 1)) - 1);
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
			case 'Shift+Digit0': case 'Shift+Numpad0':
				pressDigit(parseInt(keyCombo.substring(keyCombo.length - 1)), ev, 'corner');
				break;

			case 'Delete':
			case 'Backspace':
				clearCells();
				break;

			case 'Ctrl+Delete':
			case 'Ctrl+Backspace':
				if (selectedCells.some(c => state.enteredDigits[c] === null && state.centerNotation[c].length > 0))
				{
					saveUndo();
					for (let cell of selectedCells)
						state.centerNotation[cell] = [];
					updateVisuals(true);
				}
				break;

			case 'Shift+Delete':
			case 'Shift+Backspace':
				if (selectedCells.some(c => state.enteredDigits[c] === null && state.cornerNotation[c].length > 0))
				{
					saveUndo();
					for (let cell of selectedCells)
						state.cornerNotation[cell] = [];
					updateVisuals(true);
				}
				break;

			case 'Ctrl+Shift+Delete':
			case 'Ctrl+Shift+Backspace':
				if (selectedCells.some(c => state.colors[c].length > 0))
				{
					saveUndo();
					for (let cell of selectedCells)
						state.colors[cell] = [];
					updateVisuals(true);
				}
				break;

			case 'Ctrl+KeyC':
			case 'Ctrl+Insert':
				navigator.clipboard.writeText(selectedCells.map(c => getDisplayedSudokuDigit(state, c) ?? '.').join(''));
				break;

			// Navigation
			case 'Shift': shiftPressed = true; break;
			case 'Control': ctrlPressed = true; break;
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
			case 'Ctrl+KeyA': selectedCells = ns(width * height); updateVisuals(); break;
			case 'Equal': case 'Shift+Equal': selectSimilar(ev); break;

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

			default:
				anyFunction = false;
				//console.log(keyCombo, ev.code);
				break;
		}

		if (anyFunction)
		{
			ev.stopPropagation();
			ev.preventDefault();
			return false;
		}
	});
	puzzleContainer.addEventListener('keyup', ev =>
	{
		if (!ev.ctrlKey || !ev.shiftKey)
			puzzleContainer.classList.remove('dimmable');

		switch (ev.key)
		{
			case 'Shift':
				shiftPressed = false;
				updateVisuals();
				break;
			case 'Control':
				ctrlPressed = false;
				updateVisuals();
				break;
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
	setDynamicEvents();


	/// — RUN

	retrieveStateFromLocalStorage();

	if (puzzleId === 'test')
	{
		await updatePuzzleFromEdit();
		document.addEventListener('visibilitychange', updatePuzzleFromEdit);
	}
	else
	{
		await dotNet('SetupConstraints', [JSON.stringify(constraintTypes), JSON.stringify({ customConstraintTypes: customConstraintTypes, constraints: constraints, givens: givens, width: width, height: height, regions: regions })]);
		updateVisuals(true);
	}

	puzzleContainer.focus();
	fixViewBox();
});
