import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminAuditLog {
  adminAuditLogId: number;
  adminUserId: number;
  fullName: string;
  username: string;
  occurredAt: string;
  action: string;
  description: string;
}

@Injectable({ providedIn: 'root' })
export class AdminAuditLogsService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getRecent(take = 100): Observable<AdminAuditLog[]> {
    const params = new HttpParams().set('take', take);
    return this.http.get<AdminAuditLog[]>(`${this.apiBaseUrl}/admin/audit-logs`, { params });
  }

  recordViewEntry(viewKey: string): Observable<AdminAuditLog> {
    return this.http.post<AdminAuditLog>(`${this.apiBaseUrl}/admin/audit-logs/view-entry`, {
      viewKey
    });
  }
}
