export interface UnitRecommendation {
  unitId: string;
  projectId: string;
  unitCode: string;
  propertyType: string | null;
  price: number;
  location: string | null;
  score: number;
  matchReasons: string[];
  conversionLikelihood: number | null;
}
