(() => {
	document.querySelectorAll('[data-rich-text-editor]').forEach((editor) => {
		const surface = editor.querySelector('[contenteditable="true"]');
		const source = editor.querySelector('[data-editor-source]');
		if (!surface || !source) {
			return;
		}

		surface.innerHTML = source.value;
		surface.hidden = false;
		source.hidden = true;
		source.required = false;
		document.execCommand('styleWithCSS', false, true);

		const syncSource = () => {
			source.value = surface.innerHTML;
			source.dispatchEvent(new Event('input', { bubbles: true }));
		};

		let savedSelection;
		const saveSelection = () => {
			const selection = window.getSelection();
			if (selection?.rangeCount && surface.contains(selection.anchorNode)) {
				savedSelection = selection.getRangeAt(0).cloneRange();
			}
		};
		const restoreSelection = () => {
			if (!savedSelection || !surface.contains(savedSelection.commonAncestorContainer)) {
				surface.focus();
				return false;
			}

			surface.focus();
			const selection = window.getSelection();
			selection.removeAllRanges();
			selection.addRange(savedSelection);
			return true;
		};
		const insertNodeAtSelection = (node) => {
			restoreSelection();
			const selection = window.getSelection();
			if (selection?.rangeCount && surface.contains(selection.anchorNode)) {
				const range = selection.getRangeAt(0);
				range.deleteContents();
				range.insertNode(node);
				range.setStartAfter(node);
				range.collapse(true);
				selection.removeAllRanges();
				selection.addRange(range);
			} else {
				surface.append(node);
			}
			saveSelection();
			syncSource();
		};
		const insertImage = (url, alt = '') => {
			const image = document.createElement('img');
			image.src = url;
			image.alt = alt;
			image.loading = 'lazy';
			insertNodeAtSelection(image);
		};
		const status = editor.parentElement.querySelector('[data-editor-status]');

		surface.addEventListener('paste', async (event) => {
			const clipboard = event.clipboardData;
			if (!clipboard) {
				return;
			}

			const imageFile = Array.from(clipboard.items)
				.find((item) => item.kind === 'file' && item.type.startsWith('image/'))
				?.getAsFile();
			if (imageFile) {
				event.preventDefault();
				saveSelection();
				status.textContent = '이미지를 업로드하는 중입니다...';
				try {
					const data = new FormData();
					data.append('image', imageFile, imageFile.name || 'pasted-image');
					data.append('__RequestVerificationToken', editor.closest('form').querySelector('[name="__RequestVerificationToken"]').value);
					const response = await fetch(editor.dataset.imageUploadUrl, {
						method: 'POST',
						body: data,
						headers: { 'X-Requested-With': 'XMLHttpRequest' }
					});
					const result = await response.json();
					if (!response.ok || !result.url) {
						throw new Error(result.message || '이미지 업로드에 실패했습니다.');
					}
					insertImage(result.url, imageFile.name);
					status.textContent = '이미지를 본문에 추가했습니다.';
				} catch (error) {
					status.textContent = error.message || '이미지 업로드에 실패했습니다.';
				}
				return;
			}

			const html = clipboard.getData('text/html');
			if (!html) {
				return;
			}

			const pastedImages = Array.from(new DOMParser().parseFromString(html, 'text/html').images)
				.map((image) => ({
					src: image.getAttribute('src'),
					alt: image.getAttribute('alt') || ''
				}))
				.filter((image) => {
					if (!image.src) {
						return false;
					}

					try {
						return ['http:', 'https:'].includes(new URL(image.src, document.baseURI).protocol);
					} catch {
						return false;
					}
				});
			if (pastedImages.length === 0) {
				return;
			}

			event.preventDefault();
			saveSelection();
			pastedImages.forEach((image) => insertImage(new URL(image.src, document.baseURI).href, image.alt));
			status.textContent = '복사한 이미지를 본문에 추가했습니다.';
		});

		document.addEventListener('selectionchange', saveSelection);

		editor.querySelectorAll('[data-editor-command]').forEach((button) => {
			button.addEventListener('mousedown', (event) => event.preventDefault());
			button.addEventListener('pointerdown', saveSelection);
			button.addEventListener('click', () => {
				restoreSelection();
				document.execCommand(button.dataset.editorCommand);
				saveSelection();
				syncSource();
			});
		});

		const blockPicker = editor.querySelector('[data-editor-block]');
		blockPicker?.addEventListener('pointerdown', saveSelection);
		blockPicker?.addEventListener('change', (event) => {
			restoreSelection();
			document.execCommand('formatBlock', false, event.target.value);
			saveSelection();
			syncSource();
		});

		const colorPicker = editor.querySelector('[data-editor-color]');
		colorPicker?.addEventListener('mousedown', saveSelection);
		colorPicker?.addEventListener('pointerdown', saveSelection);
		colorPicker?.addEventListener('change', () => {
			restoreSelection();
			document.execCommand('foreColor', false, colorPicker.value);
			saveSelection();
			syncSource();
		});

		const linkButton = editor.querySelector('[data-editor-link]');
		linkButton?.addEventListener('mousedown', (event) => event.preventDefault());
		linkButton?.addEventListener('click', () => {
			const url = window.prompt('연결할 URL을 입력해 주세요.');
			if (url) {
				restoreSelection();
				document.execCommand('createLink', false, url);
				saveSelection();
				syncSource();
			}
		});

		surface.addEventListener('mouseup', saveSelection);
		surface.addEventListener('keyup', saveSelection);
		surface.addEventListener('blur', saveSelection);
		surface.addEventListener('input', syncSource);
		surface.closest('form')?.addEventListener('submit', syncSource);
	});
})();
