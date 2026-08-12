import { useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Pagination } from "../../components/Pagination";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { useConfirmDialog } from "../../components/ConfirmDialog";
import { UnitForm, type UnitFormValues } from "./UnitForm";
import { useCreateUnit, useDeleteUnit, useUnits, useUpdateUnit } from "./unitsApi";
import type { Unit, UnitListQuery, UnitStatus } from "../../types/unit";
import { getApiErrorMessage } from "../../api/client";
import { formatCurrency } from "../../utils/format";

export function UnitsListPage() {
  const [query, setQuery] = useState<UnitListQuery>({ page: 1, pageSize: 20 });
  const [editing, setEditing] = useState<Unit | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data, isLoading, isError } = useUnits(query);
  const createUnit = useCreateUnit();
  const updateUnit = useUpdateUnit(editing?.id ?? "");
  const deleteUnit = useDeleteUnit();
  const { confirm, dialog: confirmDialog } = useConfirmDialog();

  const toRequest = (values: UnitFormValues) => ({
    projectId: values.projectId,
    unitCode: values.unitCode,
    propertyType: values.propertyType || null,
    price: values.price,
    area: values.area ?? null,
    bedrooms: values.bedrooms ?? null,
    bathrooms: values.bathrooms ?? null,
    floor: values.floor || null,
    location: values.location || null,
    status: values.status as UnitStatus,
    downPayment: values.downPayment ?? null,
    installmentYears: values.installmentYears ?? null,
    description: values.description || null,
    isPubliclyListed: values.isPubliclyListed ?? false,
  });

  const handleCreate = async (values: UnitFormValues) => {
    setError(null);
    try {
      await createUnit.mutateAsync(toRequest(values));
      setShowCreate(false);
      toast.success("Unit created");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create unit."));
    }
  };

  const handleUpdate = async (values: UnitFormValues) => {
    setError(null);
    try {
      await updateUnit.mutateAsync(toRequest(values));
      setEditing(null);
      toast.success("Unit updated");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not update unit."));
    }
  };

  const handleDelete = async (unit: Unit) => {
    if (!(await confirm({ message: `Delete unit "${unit.unitCode}"? This cannot be undone.`, confirmLabel: "Delete" }))) return;
    try {
      await deleteUnit.mutateAsync(unit.id);
      toast.success("Unit deleted");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not delete unit."));
    }
  };

  return (
    <>
      <PageHeader
        title="Units"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Unit
          </button>
        }
      />

      <div className="toolbar">
        <input
          className="input"
          style={{ maxWidth: 260 }}
          placeholder="Search unit code, location…"
          onChange={(e) => setQuery((q) => ({ ...q, search: e.target.value, page: 1 }))}
        />
      </div>

      {error && <p className="field-error">{error}</p>}

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load units."
        isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
        emptyMessage="No units yet."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Price</th>
                <th>Bedrooms</th>
                <th>Status</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((unit) => (
                <tr key={unit.id}>
                  <td>
                    <Link to={`/units/${unit.id}`}>{unit.unitCode}</Link>
                  </td>
                  <td>{formatCurrency(unit.price)}</td>
                  <td>{unit.bedrooms ?? "—"}</td>
                  <td>
                    <StatusBadge status={unit.status} />
                  </td>
                  <td>
                    <button type="button" className="btn" onClick={() => setEditing(unit)}>
                      Edit
                    </button>{" "}
                    <button type="button" className="btn btn-danger" onClick={() => handleDelete(unit)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={data?.page ?? 1}
          totalPages={data?.totalPages ?? 1}
          onPageChange={(page) => setQuery((q) => ({ ...q, page }))}
        />
      </AsyncState>

      {showCreate && (
        <Modal title="New Unit" onClose={() => setShowCreate(false)}>
          <UnitForm onSubmit={handleCreate} submitLabel="Create Unit" />
        </Modal>
      )}

      {editing && (
        <Modal title="Edit Unit" onClose={() => setEditing(null)}>
          <UnitForm
            defaultValues={{
              projectId: editing.projectId,
              unitCode: editing.unitCode,
              propertyType: editing.propertyType ?? "",
              price: editing.price,
              area: editing.area ?? undefined,
              bedrooms: editing.bedrooms ?? undefined,
              bathrooms: editing.bathrooms ?? undefined,
              floor: editing.floor ?? "",
              location: editing.location ?? "",
              status: editing.status,
              downPayment: editing.downPayment ?? undefined,
              installmentYears: editing.installmentYears ?? undefined,
              description: editing.description ?? "",
              isPubliclyListed: editing.isPubliclyListed,
            }}
            onSubmit={handleUpdate}
            submitLabel="Save Changes"
          />
        </Modal>
      )}

      {confirmDialog}
    </>
  );
}
