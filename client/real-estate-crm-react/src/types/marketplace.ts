export interface PublicUnit {
  unitId: string;
  unitCode: string;
  propertyType: string | null;
  price: number;
  area: number | null;
  bedrooms: number | null;
  bathrooms: number | null;
  location: string | null;
  description: string | null;
  projectName: string;
  companyName: string;
}

export interface PublicUnitListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  propertyType?: string;
  location?: string;
  minPrice?: number;
  maxPrice?: number;
}
