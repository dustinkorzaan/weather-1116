/** Formats a build timestamp like "7/12/2026 10:22:14 PM UTC" (matches MVC and Blazor). */
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

export function AboutTreeNode({ node }) {
  if (!node) {
    return null;
  }

  const hasChildren = Array.isArray(node.children) && node.children.length > 0;
  const metadata = [];
  if (Number.isFinite(node.buildNumber)) {
    metadata.push({ text: `Build #${node.buildNumber}`, value: node.buildNumber });
  }
  if (node.buildStart) {
    metadata.push({ text: `Started ${formatBuildStart(node.buildStart)}`, value: formatBuildStart(node.buildStart) });
  }
  if (node.buildBranchName) {
    metadata.push({ text: `Branch ${node.buildBranchName}`, value: node.buildBranchName, isBranch: true });
  }

  return (
    <li className="my-2">
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-semibold text-foreground">{node.name ?? 'Unnamed node'}</span>
        <span
          className={`rounded-full px-2 py-0.5 text-[0.7rem] font-bold tracking-wide uppercase ${
            node.isHealthy
              ? 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-300'
              : 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300'
          }`}
        >
          {node.isHealthy ? 'Healthy' : 'Unhealthy'}
        </span>
      </div>
      {node.publicMessage && <div className="mt-1 text-xs text-muted-foreground">{node.publicMessage}</div>}
      {metadata.length > 0 && (
        <div className="mt-1 text-xs text-muted-foreground">
          {metadata.map((item, index) => (
            <span key={`${item.text}-${index}`}>
              {index > 0 && ' | '}
              <span className={item.isBranch && item.value !== 'main' ? 'text-amber-600 dark:text-amber-400' : undefined}>
                {item.text}
              </span>
            </span>
          ))}
        </div>
      )}

      {hasChildren && (
        <ul className="mt-1 list-disc pl-5">
          {node.children.map((child, index) => (
            <AboutTreeNode key={`${child.name ?? 'node'}-${index}`} node={child} />
          ))}
        </ul>
      )}
    </li>
  );
}
