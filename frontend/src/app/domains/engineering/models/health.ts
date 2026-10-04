export interface EngineeringHealthHistoryPoint {
  periodStart: string;
  periodEnd: string;
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
}

export interface EngineeringHealthScore {
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
}
