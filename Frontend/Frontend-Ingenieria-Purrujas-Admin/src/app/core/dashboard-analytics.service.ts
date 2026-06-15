import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface KpiSummary {
  reservationsThisMonth: number;
  confirmedThisMonth: number;
  revenueThisMonthUsd: number;
  totalRooms: number;
  occupiedRooms: number;
  availableRooms: number;
}

export interface MonthlyDataPoint {
  year: number;
  month: number;
  count: number;
  amount: number;
}

export interface StatusCount {
  status: string;
  count: number;
}

export interface RoomStatusSummary {
  available: number;
  occupied: number;
  maintenance: number;
  total: number;
}

export interface RoomTypeCount {
  roomTypeName: string;
  reservationCount: number;
}

export interface DashboardMetrics {
  kpi: KpiSummary;
  reservationsByMonth: MonthlyDataPoint[];
  revenueByMonth: MonthlyDataPoint[];
  reservationsByStatus: StatusCount[];
  roomStatus: RoomStatusSummary;
  topRoomTypes: RoomTypeCount[];
}

@Injectable({ providedIn: 'root' })
export class DashboardAnalyticsService {
  private readonly apiBase = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getMetrics(): Observable<DashboardMetrics> {
    return this.http.get<DashboardMetrics>(`${this.apiBase}/dashboard/metrics`);
  }
}
