interface BreakdownTableProps {
  title: string;
  data: Record<string, number>;
}

export function BreakdownTable({ title, data }: BreakdownTableProps) {
  const entries = Object.entries(data);

  return (
    <div className="card">
      <h3 style={{ marginTop: 0 }}>{title}</h3>
      {entries.length === 0 ? (
        <p className="state-message">No data.</p>
      ) : (
        <table className="table">
          <tbody>
            {entries.map(([key, value]) => (
              <tr key={key}>
                <td>{key}</td>
                <td>{value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
