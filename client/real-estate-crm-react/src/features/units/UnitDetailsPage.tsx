import { useParams } from "react-router-dom";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { useUnit } from "./unitsApi";

export function UnitDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: unit, isLoading, isError } = useUnit(id);

  return (
    <>
      <PageHeader title={unit?.unitCode ?? "Unit Details"} />
      <AsyncState isLoading={isLoading} isError={isError} errorMessage="Failed to load unit.">
        {unit && (
          <div className="card" style={{ maxWidth: 480 }}>
            <p>
              <strong>Status:</strong> <StatusBadge status={unit.status} />
            </p>
            <p>
              <strong>Price:</strong> {unit.price}
            </p>
            <p>
              <strong>Area:</strong> {unit.area ?? "—"} sqm
            </p>
            <p>
              <strong>Bedrooms / Bathrooms:</strong> {unit.bedrooms ?? "—"} / {unit.bathrooms ?? "—"}
            </p>
            <p>
              <strong>Floor:</strong> {unit.floor ?? "—"}
            </p>
            <p>
              <strong>Location:</strong> {unit.location ?? "—"}
            </p>
            <p>
              <strong>Down payment:</strong> {unit.downPayment ?? "—"}
            </p>
            <p>
              <strong>Installment years:</strong> {unit.installmentYears ?? "—"}
            </p>
            <p>
              <strong>Description:</strong> {unit.description ?? "—"}
            </p>
          </div>
        )}
      </AsyncState>
    </>
  );
}
