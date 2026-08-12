export const UnitStatus = {
  Available: "Available",
  Reserved: "Reserved",
  Sold: "Sold",
  Unavailable: "Unavailable",
} as const;
export type UnitStatus = (typeof UnitStatus)[keyof typeof UnitStatus];

export interface Unit {
  id: string;
  projectId: string;
  unitCode: string;
  propertyType: string | null;
  price: number;
  area: number | null;
  bedrooms: number | null;
  bathrooms: number | null;
  floor: string | null;
  location: string | null;
  status: UnitStatus;
  downPayment: number | null;
  installmentYears: number | null;
  description: string | null;
  isPubliclyListed: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUnitRequest {
  projectId: string;
  unitCode: string;
  propertyType?: string | null;
  price: number;
  area?: number | null;
  bedrooms?: number | null;
  bathrooms?: number | null;
  floor?: string | null;
  location?: string | null;
  status: UnitStatus;
  downPayment?: number | null;
  installmentYears?: number | null;
  description?: string | null;
  isPubliclyListed?: boolean;
}

export type UpdateUnitRequest = CreateUnitRequest;

export interface UnitListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: UnitStatus;
  projectId?: string;
  propertyType?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface UnitImage {
  id: string;
  unitId: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}
