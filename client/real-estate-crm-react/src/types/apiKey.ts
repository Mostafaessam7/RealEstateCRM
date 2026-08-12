export interface ApiKey {
  id: string;
  name: string;
  keyPrefix: string;
  scopes: string;
  isActive: boolean;
  lastUsedAt: string | null;
  expiresAt: string | null;
  createdAt: string;
}

export interface CreatedApiKey extends ApiKey {
  plaintextKey: string;
}

export interface CreateApiKeyRequest {
  name: string;
  scopes: string;
  expiresAt?: string;
}
