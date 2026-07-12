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

	function createNode(node) {
		const li = document.createElement('li');
		li.className = 'about-tree-item';

		const row = document.createElement('div');
		row.className = 'about-tree-row';

		const name = document.createElement('span');
		name.className = 'about-tree-name';
		name.textContent = node?.name ?? 'Unnamed node';

		const health = document.createElement('span');
		const isHealthy = !!node?.isHealthy;
		health.className = `about-tree-health ${isHealthy ? 'healthy' : 'unhealthy'}`;
		health.textContent = isHealthy ? 'Healthy' : 'Unhealthy';

		row.appendChild(name);
		row.appendChild(health);
		li.appendChild(row);

		const metadata = [];
		if (Number.isFinite(node?.buildNumber)) {
			metadata.push(`Build #${node.buildNumber}`);
		}
		if (node?.buildStart) {
			metadata.push(`Started ${formatBuildStart(node.buildStart)}`);
		}
		if (metadata.length > 0) {
			const meta = document.createElement('div');
			meta.className = 'about-tree-meta';
			meta.textContent = metadata.join(' | ');
			li.appendChild(meta);
		}

		if (Array.isArray(node?.children) && node.children.length > 0) {
			const childList = document.createElement('ul');
			childList.className = 'about-tree-list';

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

		status.textContent = 'Loading About information...';
		status.classList.remove('d-none', 'about-status-error');
		container.classList.add('d-none');
		container.textContent = '';

		try {
			const response = await fetch('/About');
			if (!response.ok) {
				throw new Error(`Request failed: ${response.status}`);
			}

			const root = await response.json();
			const rootList = document.createElement('ul');
			rootList.className = 'about-tree-list root';
			rootList.appendChild(createNode(root));

			container.appendChild(rootList);
			container.classList.remove('d-none');
			status.classList.add('d-none');
		} catch {
			status.textContent = 'Unable to load About information.';
			status.classList.add('about-status-error');
		}
	}

	document.addEventListener('DOMContentLoaded', () => {
		const aboutModal = document.getElementById('aboutModal');
		if (!aboutModal) {
			return;
		}

		aboutModal.addEventListener('show.bs.modal', loadAbout);
	});
})();
