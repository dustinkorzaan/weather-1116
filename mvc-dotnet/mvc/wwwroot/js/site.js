// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
(function initAboutModal() {
	// Formats a build timestamp like "7/12/2026 10:22:14 PM UTC" (matches React and Blazor).
	function formatBuildStart(isoDate) {
		const date = new Date(isoDate);
		if (Number.isNaN(date.getTime())) {
			return String(isoDate);
		}

		const month = date.getUTCMonth() + 1;
		const day = date.getUTCDate();
		const year = date.getUTCFullYear();
		const minutes = String(date.getUTCMinutes()).padStart(2, '0');
		const seconds = String(date.getUTCSeconds()).padStart(2, '0');
		const period = date.getUTCHours() >= 12 ? 'PM' : 'AM';
		const hours = date.getUTCHours() % 12 || 12;

		return `${month}/${day}/${year} ${hours}:${minutes}:${seconds} ${period} UTC`;
	}

	function setHidden(el, hidden) {
		if (!el) {
			return;
		}
		if (hidden) {
			el.setAttribute('hidden', '');
		} else {
			el.removeAttribute('hidden');
		}
	}

	function createNode(node) {
		const li = document.createElement('li');
		li.className = 'about-node';

		const row = document.createElement('div');
		row.className = 'about-node-row';

		const name = document.createElement('span');
		name.className = 'about-node-name';
		name.textContent = node?.name ?? 'Unnamed node';

		const health = document.createElement('span');
		const isHealthy = !!node?.isHealthy;
		health.className = `about-health ${isHealthy ? 'is-healthy' : 'is-unhealthy'}`;
		health.textContent = isHealthy ? 'Healthy' : 'Unhealthy';

		row.appendChild(name);
		row.appendChild(health);
		li.appendChild(row);

		if (node?.publicMessage) {
			const message = document.createElement('div');
			message.className = 'about-node-message';
			message.textContent = node.publicMessage;
			li.appendChild(message);
		}

		const metadata = [];
		if (Number.isFinite(node?.buildNumber)) {
			metadata.push({ text: `Build #${node.buildNumber}`, value: node.buildNumber });
		}
		if (node?.buildStart) {
			metadata.push({ text: `Started ${formatBuildStart(node.buildStart)}`, value: formatBuildStart(node.buildStart) });
		}
		if (node?.buildBranchName) {
			metadata.push({ text: `Branch ${node.buildBranchName}`, value: node.buildBranchName, isBranch: true });
		}
		if (metadata.length > 0) {
			const meta = document.createElement('div');
			meta.className = 'about-node-meta';
			metadata.forEach((item, index) => {
				if (index > 0) {
					meta.appendChild(document.createTextNode(' | '));
				}
				const value = document.createElement('span');
				value.textContent = item.text;
				if (item.isBranch && item.value !== 'main') {
					value.className = 'is-non-main-branch';
				}
				meta.appendChild(value);
			});
			li.appendChild(meta);
		}

		if (Array.isArray(node?.children) && node.children.length > 0) {
			const childList = document.createElement('ul');
			childList.className = 'about-children';

			node.children.forEach((child) => {
				childList.appendChild(createNode(child));
			});

			li.appendChild(childList);
		}

		return li;
	}

	async function loadAbout() {
		const status = document.getElementById('aboutStatus');
		const container = document.getElementById('aboutTreeContainer');
		if (!status || !container) {
			return;
		}

		status.innerHTML = '<span class="spinner spinner-on-muted" aria-hidden="true"></span><span>Loading About information...</span>';
		status.className = 'about-status';
		setHidden(container, true);
		container.textContent = '';

		try {
			const response = await fetch('/About');
			if (!response.ok) {
				throw new Error(`Request failed: ${response.status}`);
			}

			const root = await response.json();
			const rootList = document.createElement('ul');
			rootList.className = 'about-tree';
			rootList.appendChild(createNode(root));

			container.appendChild(rootList);
			setHidden(container, false);
			setHidden(status, true);
		} catch {
			status.textContent = 'Unable to load About information.';
			status.className = 'about-status is-error';
			setHidden(status, false);
		}
	}

	document.addEventListener('DOMContentLoaded', () => {
		const menuButton = document.getElementById('avatarMenuButton');
		const menu = document.getElementById('avatarMenu');
		const aboutMenuItem = document.getElementById('aboutMenuItem');
		const aboutModal = document.getElementById('aboutModal');
		const aboutModalClose = document.getElementById('aboutModalClose');

		function closeMenu() {
			menu?.classList.remove('is-open');
			menuButton?.setAttribute('aria-expanded', 'false');
		}

		function openMenu() {
			menu?.classList.add('is-open');
			menuButton?.setAttribute('aria-expanded', 'true');
		}

		function isModalOpen() {
			return !!aboutModal && aboutModal.classList.contains('is-open');
		}

		function closeModal() {
			aboutModal?.classList.remove('is-open');
		}

		function openModal() {
			if (!aboutModal) {
				return;
			}

			aboutModal.classList.add('is-open');
			loadAbout();
		}

		menuButton?.addEventListener('click', (event) => {
			event.stopPropagation();
			if (menu?.classList.contains('is-open')) {
				closeMenu();
			} else {
				openMenu();
			}
		});

		menu?.addEventListener('click', () => closeMenu());

		document.addEventListener('click', (event) => {
			if (menu && !menu.contains(event.target) && event.target !== menuButton) {
				closeMenu();
			}
		});

		aboutMenuItem?.addEventListener('click', openModal);
		aboutModalClose?.addEventListener('click', closeModal);

		// Backdrop click closes; clicks inside the dialog do not bubble to the backdrop element itself.
		aboutModal?.addEventListener('click', (event) => {
			if (event.target === aboutModal) {
				closeModal();
			}
		});

		document.addEventListener('keydown', (event) => {
			if (event.key !== 'Escape') {
				return;
			}

			if (isModalOpen()) {
				closeModal();
			} else {
				closeMenu();
			}
		});
	});
})();
