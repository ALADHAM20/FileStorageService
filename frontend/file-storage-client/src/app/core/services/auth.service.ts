import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export type UserRole = 'user' | 'admin';

export interface MockTokenRequest {
  userId: string;
  role: UserRole;
}

export interface MockTokenResponse {
  accessToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenKey = 'file-storage-token';
  private readonly userIdKey = 'file-storage-user-id';
  private readonly roleKey = 'file-storage-role';

  constructor(private readonly http: HttpClient) {}

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  get userId(): string | null {
    return localStorage.getItem(this.userIdKey);
  }

  get role(): UserRole | null {
    return localStorage.getItem(this.roleKey) as UserRole | null;
  }

  get isLoggedIn(): boolean {
    return !!this.token;
  }

  login(request: MockTokenRequest): Observable<MockTokenResponse> {
    return this.http
      .post<MockTokenResponse>(`${environment.apiBaseUrl}/api/auth/mock-token`, request)
      .pipe(
        tap((response) => {
          localStorage.setItem(this.tokenKey, response.accessToken);
          localStorage.setItem(this.userIdKey, request.userId);
          localStorage.setItem(this.roleKey, request.role);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userIdKey);
    localStorage.removeItem(this.roleKey);
  }
}
