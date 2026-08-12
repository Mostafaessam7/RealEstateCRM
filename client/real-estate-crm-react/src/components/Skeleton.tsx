export function Skeleton({ width = "100%", height = 14, radius = 6 }: { width?: string | number; height?: number; radius?: number }) {
  return <div className="skeleton" style={{ width, height, borderRadius: radius }} />;
}

export function TableSkeleton({ rows = 6, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            {Array.from({ length: columns }).map((_, i) => (
              <th key={i}>
                <Skeleton width={70 + (i % 3) * 20} height={10} />
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {Array.from({ length: rows }).map((_, r) => (
            <tr key={r}>
              {Array.from({ length: columns }).map((_, c) => (
                <td key={c}>
                  <Skeleton width={c === 0 ? 140 : 80 + (c % 3) * 30} />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function CardGridSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="kpi-grid">
      {Array.from({ length: count }).map((_, i) => (
        <div className="card" key={i}>
          <Skeleton width={36} height={36} radius={10} />
          <div style={{ marginTop: 14 }}>
            <Skeleton width={70} height={22} />
          </div>
          <div style={{ marginTop: 8 }}>
            <Skeleton width={100} height={11} />
          </div>
        </div>
      ))}
    </div>
  );
}
