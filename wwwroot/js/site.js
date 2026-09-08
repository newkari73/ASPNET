// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

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
	const modal = document.querySelector('[data-schedule-modal]');
	const openButton = document.querySelector('[data-schedule-open]');
	const form = document.querySelector('[data-schedule-form]');

	if (!modal || !openButton || !form) {
		return;
	}

	const error = modal.querySelector('[data-schedule-error]');
	const closeModal = () => {
		modal.hidden = true;
		document.body.classList.remove('modal-open');
	};

	openButton.addEventListener('click', () => {
		error.textContent = '';
		modal.hidden = false;
		document.body.classList.add('modal-open');
		modal.querySelector('[name="Title"]').focus();
	});

	modal.querySelectorAll('[data-schedule-close]').forEach((button) => {
		button.addEventListener('click', closeModal);
	});

	form.addEventListener('submit', async (event) => {
		event.preventDefault();
		error.textContent = '';
		const submitButton = form.querySelector('button[type="submit"]');
		submitButton.disabled = true;

		try {
			const response = await fetch(form.action, {
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
				throw new Error(result.message || '일정 등록에 실패했습니다.');
			}
			window.location.href = `/Schedule/Index?selectedDate=${encodeURIComponent(result.selectedDate)}`;
		} catch (requestError) {
			error.textContent = requestError.message;
			submitButton.disabled = false;
		}
	});

	document.addEventListener('keydown', (event) => {
		if (event.key === 'Escape' && !modal.hidden) {
			closeModal();
		}
	});
})();
