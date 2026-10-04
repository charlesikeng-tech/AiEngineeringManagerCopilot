export interface ReportHistoryItem {
  id: string;
  teamId: string;
  periodStart: string;
  periodEnd: string;
  overallScore: number;
  healthLevel: string;
  dataCoverage: number;
  createdAt: string;
  scoreDelta: number | null;
  coverageDelta: number | null;
}
