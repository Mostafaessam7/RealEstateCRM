import { useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { UserForm, type UserFormValues } from "./UserForm";
import { useCreateUser, useUpdateUserActive, useUpdateUserRole, useUsers } from "./usersApi";
import { Roles, type Role } from "../../types/auth";
import { getApiErrorMessage } from "../../api/client";

export function UsersListPage() {
  const { data: users, isLoading, isError } = useUsers();
  const createUser = useCreateUser();
  const updateRole = useUpdateUserRole();
  const updateActive = useUpdateUserActive();

  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCreate = async (values: UserFormValues) => {
    setError(null);
    try {
      await createUser.mutateAsync({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        role: values.role as Role,
      });
      setShowCreate(false);
      toast.success("User created");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create user."));
    }
  };

  const handleRoleChange = async (id: string, role: Role) => {
    setError(null);
    try {
      await updateRole.mutateAsync({ id, request: { role } });
      toast.success("Role updated");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not update role."));
    }
  };

  const handleToggleActive = async (id: string, isActive: boolean) => {
    setError(null);
    try {
      await updateActive.mutateAsync({ id, request: { isActive: !isActive } });
      toast.success("User updated");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not update user."));
    }
  };

  return (
    <>
      <PageHeader
        title="Users"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New User
          </button>
        }
      />

      {error && <p className="field-error">{error}</p>}

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load users."
        isEmpty={!isLoading && (users?.length ?? 0) === 0}
        emptyMessage="No users yet."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Status</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users?.map((user) => (
                <tr key={user.id}>
                  <td>{user.fullName}</td>
                  <td>{user.email}</td>
                  <td>
                    <select
                      className="input"
                      style={{ maxWidth: 160 }}
                      value={user.roles[0] ?? Roles.SalesAgent}
                      onChange={(e) => handleRoleChange(user.id, e.target.value as Role)}
                    >
                      {Object.values(Roles).map((role) => (
                        <option key={role} value={role}>
                          {role}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <StatusBadge status={user.isActive ? "Active" : "Inactive"} />
                  </td>
                  <td>
                    <button type="button" className="btn" onClick={() => handleToggleActive(user.id, user.isActive)}>
                      {user.isActive ? "Deactivate" : "Activate"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </AsyncState>

      {showCreate && (
        <Modal title="New User" onClose={() => setShowCreate(false)}>
          <UserForm onSubmit={handleCreate} />
        </Modal>
      )}
    </>
  );
}
