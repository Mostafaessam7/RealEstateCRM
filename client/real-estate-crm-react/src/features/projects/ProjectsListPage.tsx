import { useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Pagination } from "../../components/Pagination";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { useConfirmDialog } from "../../components/ConfirmDialog";
import { ProjectForm, type ProjectFormValues } from "./ProjectForm";
import { useCreateProject, useDeleteProject, useProjects, useUpdateProject } from "./projectsApi";
import type { Project, ProjectListQuery, ProjectStatus } from "../../types/project";
import { getApiErrorMessage } from "../../api/client";
import { formatCurrency } from "../../utils/format";

export function ProjectsListPage() {
  const [query, setQuery] = useState<ProjectListQuery>({ page: 1, pageSize: 20 });
  const [editing, setEditing] = useState<Project | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data, isLoading, isError } = useProjects(query);
  const createProject = useCreateProject();
  const updateProject = useUpdateProject(editing?.id ?? "");
  const deleteProject = useDeleteProject();
  const { confirm, dialog: confirmDialog } = useConfirmDialog();

  const toRequest = (values: ProjectFormValues) => ({
    name: values.name,
    developer: values.developer || null,
    location: values.location || null,
    description: values.description || null,
    startingPrice: values.startingPrice ?? null,
    status: values.status as ProjectStatus,
  });

  const handleCreate = async (values: ProjectFormValues) => {
    setError(null);
    try {
      await createProject.mutateAsync(toRequest(values));
      setShowCreate(false);
      toast.success("Project created");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create project."));
    }
  };

  const handleUpdate = async (values: ProjectFormValues) => {
    setError(null);
    try {
      await updateProject.mutateAsync(toRequest(values));
      setEditing(null);
      toast.success("Project updated");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not update project."));
    }
  };

  const handleDelete = async (project: Project) => {
    if (!(await confirm({ message: `Delete project "${project.name}"? This cannot be undone.`, confirmLabel: "Delete" }))) return;
    try {
      await deleteProject.mutateAsync(project.id);
      toast.success("Project deleted");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not delete project."));
    }
  };

  return (
    <>
      <PageHeader
        title="Projects"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Project
          </button>
        }
      />

      <div className="toolbar">
        <input
          className="input"
          style={{ maxWidth: 260 }}
          placeholder="Search projects…"
          onChange={(e) => setQuery((q) => ({ ...q, search: e.target.value, page: 1 }))}
        />
      </div>

      {error && <p className="field-error">{error}</p>}

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load projects."
        isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
        emptyMessage="No projects yet."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Developer</th>
                <th>Location</th>
                <th>Starting price</th>
                <th>Status</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((project) => (
                <tr key={project.id}>
                  <td>{project.name}</td>
                  <td>{project.developer ?? "—"}</td>
                  <td>{project.location ?? "—"}</td>
                  <td>{project.startingPrice != null ? `$${formatCurrency(project.startingPrice)}` : "—"}</td>
                  <td>
                    <StatusBadge status={project.status} />
                  </td>
                  <td>
                    <button type="button" className="btn" onClick={() => setEditing(project)}>
                      Edit
                    </button>{" "}
                    <button type="button" className="btn btn-danger" onClick={() => handleDelete(project)}>
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
        <Modal title="New Project" onClose={() => setShowCreate(false)}>
          <ProjectForm onSubmit={handleCreate} submitLabel="Create Project" />
        </Modal>
      )}

      {editing && (
        <Modal title="Edit Project" onClose={() => setEditing(null)}>
          <ProjectForm
            defaultValues={{
              name: editing.name,
              developer: editing.developer ?? "",
              location: editing.location ?? "",
              description: editing.description ?? "",
              startingPrice: editing.startingPrice ?? undefined,
              status: editing.status,
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
