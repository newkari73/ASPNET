// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
	document.querySelectorAll('[data-image-preview]').forEach((input) => {
		const previewId = input.dataset.imagePreview;
		const preview = previewId ? document.getElementById(previewId) : null;
		if (!preview) {
			return;
		}

		input.addEventListener('change', () => {
			const file = input.files && input.files[0];
			if (!file || !file.type.startsWith('image/')) {
				preview.removeAttribute('src');
				preview.hidden = true;
				return;
			}

			const previewUrl = URL.createObjectURL(file);
			preview.src = previewUrl;
			preview.hidden = false;
		});
	});

	document.querySelectorAll('[data-photo-input]').forEach((input) => {
		const previewRoot = document.getElementById(input.dataset.photoInput);
		if (!previewRoot) {
			return;
		}

		input.addEventListener('change', () => {
			const files = Array.from(input.files || []);
			previewRoot.innerHTML = '';

			if (files.length === 0) {
				return;
			}

			files.forEach((file) => {
				if (!file.type.startsWith('image/')) {
					return;
				}

				const img = document.createElement('img');
				img.src = URL.createObjectURL(file);
				img.alt = file.name;
				previewRoot.appendChild(img);
			});
		});
	});
})();

(() => {
	const modal = document.querySelector('[data-file-modal]');
	const openButton = document.querySelector('[data-file-modal-open]');

	if (!modal || !openButton) {
		return;
	}

	const frame = modal.querySelector('[data-file-modal-frame]');
	const image = modal.querySelector('[data-file-modal-image]');
	const sizeButtons = modal.querySelectorAll('[data-file-modal-size]');
	const imageExtensions = /\.(avif|bmp|gif|jpe?g|png|svg|webp)$/i;

	const closeModal = () => {
		modal.hidden = true;
		frame.removeAttribute('src');
		image.removeAttribute('src');
		image.hidden = true;
		frame.hidden = false;
		document.body.classList.remove('modal-open');
	};

	openButton.addEventListener('click', () => {
		const fileUrl = openButton.dataset.fileUrl;
		if (imageExtensions.test(fileUrl)) {
			image.src = fileUrl;
			image.hidden = false;
			frame.hidden = true;
		} else {
			frame.src = fileUrl;
			frame.hidden = false;
			image.hidden = true;
		}
		modal.hidden = false;
		document.body.classList.add('modal-open');
	});

	modal.querySelectorAll('[data-file-modal-close]').forEach((button) => {
		button.addEventListener('click', closeModal);
	});

	sizeButtons.forEach((button) => {
		button.addEventListener('click', () => {
			modal.querySelector('.file-modal-dialog').dataset.fileModalSize = button.dataset.fileModalSize;
			sizeButtons.forEach((item) => item.classList.toggle('active', item === button));
		});
	});

	document.addEventListener('keydown', (event) => {
		if (event.key === 'Escape' && !modal.hidden) {
			closeModal();
		}
	});
})();

(() => {
	const refreshScheduleLayout = async (selectedDate) => {
		const url = new URL(window.location.href);
		url.searchParams.set('month', selectedDate);
		url.searchParams.set('selectedDate', selectedDate);
		const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
		if (!response.ok) {
			throw new Error('일정 화면을 새로 불러오지 못했습니다.');
		}

		const html = await response.text();
		const nextLayout = new DOMParser().parseFromString(html, 'text/html').querySelector('.schedule-layout');
		const currentLayout = document.querySelector('.schedule-layout');
		if (!nextLayout || !currentLayout) {
			throw new Error('일정 화면을 갱신하지 못했습니다.');
		}

		currentLayout.replaceWith(nextLayout);
		window.history.replaceState({}, '', `${url.pathname}${url.search}`);
	};

	document.addEventListener('click', async (event) => {
		const calendarDate = event.target.closest('[data-schedule-select-date]');
		if (calendarDate) {
			event.preventDefault();
			try {
				await refreshScheduleLayout(calendarDate.dataset.scheduleSelectDate);
			} catch (requestError) {
				alert(requestError.message);
			}
			return;
		}

		const button = event.target.closest('button');
		if (!button) {
			return;
		}

		const form = document.querySelector('[data-schedule-form]');
		if (button.matches('[data-schedule-edit-id]') && form) {
			const error = form.querySelector('[data-schedule-error]');
			const submitButton = form.querySelector('button[type="submit"]');
			error.textContent = '';
			form.dataset.mode = 'edit';
			form.querySelector('[name="id"]').value = button.dataset.scheduleEditId;
			form.querySelector('[name="StartDate"]').value = button.dataset.scheduleEditStartDate;
			form.querySelector('[name="StartTime"]').value = button.dataset.scheduleEditStartTime;
			form.querySelector('[name="EndDate"]').value = button.dataset.scheduleEditEndDate;
			form.querySelector('[name="EndTime"]').value = button.dataset.scheduleEditEndTime;
			form.querySelector('[name="Title"]').value = button.dataset.scheduleEditTitle;
			form.querySelector('[name="Contents"]').value = button.closest('.schedule-item').querySelector('[data-schedule-edit-contents]').content.textContent;
			form.querySelector('[data-schedule-form-title]').textContent = '일정 수정';
			submitButton.textContent = '수정하기';
			submitButton.disabled = false;
			form.querySelector('[data-schedule-past-notice]')?.setAttribute('hidden', '');
			form.querySelector('[name="Title"]').focus();
			return;
		}

		if (!button.matches('[data-schedule-delete-id]') || !form) {
			return;
		}

		event.preventDefault();
		event.stopPropagation();
		if (!confirm('이 일정을 삭제할까요?')) {
			return;
		}

		const data = new FormData();
		data.append('id', button.dataset.scheduleDeleteId);
		data.append('selectedDate', button.dataset.scheduleDeleteDate);
		data.append('__RequestVerificationToken', form.querySelector('[name="__RequestVerificationToken"]').value);
		button.disabled = true;
		try {
			const response = await fetch('/Schedule/Delete', {
				method: 'POST',
				body: data,
				headers: { 'X-Requested-With': 'XMLHttpRequest' }
			});
			const result = await response.json();
			if (!response.ok || !result.success) {
				throw new Error(result.message || '일정 삭제에 실패했습니다.');
			}
			await refreshScheduleLayout(button.dataset.scheduleDeleteDate);
		} catch (requestError) {
			button.disabled = false;
			alert(requestError.message);
		}
	});

	document.addEventListener('submit', async (event) => {
		const form = event.target;
		if (!form.matches('[data-schedule-form]')) {
			return;
		}

		event.preventDefault();
		const error = form.querySelector('[data-schedule-error]');
		const submitButton = form.querySelector('button[type="submit"]');
		error.textContent = '';
		submitButton.disabled = true;

		try {
			const action = form.dataset.mode === 'edit'
				? form.dataset.updateUrlTemplate.replace('__id__', encodeURIComponent(form.querySelector('[name="id"]').value))
				: form.action;
			const response = await fetch(action, {
				method: 'POST',
				body: new FormData(form),
				headers: { 'X-Requested-With': 'XMLHttpRequest' }
			});
			const responseText = await response.text();
			let result;
			try {
				result = JSON.parse(responseText);
			} catch {
				throw new Error('서버 응답을 처리하지 못했습니다. 잠시 후 다시 시도해 주세요.');
			}
			if (!response.ok || !result.success) {
				throw new Error(result.message || '일정 저장에 실패했습니다.');
			}
			try {
				await refreshScheduleLayout(result.selectedDate);
			} catch {
				const url = new URL(window.location.href);
				url.searchParams.set('month', result.selectedDate);
				url.searchParams.set('selectedDate', result.selectedDate);
				window.location.assign(url);
			}
		} catch (requestError) {
			error.textContent = requestError.message;
			submitButton.disabled = false;
		}
	});
})();
