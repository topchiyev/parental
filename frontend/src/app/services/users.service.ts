import { HttpClient } from '@angular/common/http';
import { Injectable } from "@angular/core";
import { EnvironmentService } from "./environment.service";
import { User } from '../models/entity/User';
import { AuthenticationService } from './authentication.service';

@Injectable({ providedIn: 'root' })
export class UsersService {

  constructor(
    private environmentService: EnvironmentService,
    private authenticationService: AuthenticationService,
    private http: HttpClient
  ) { }

  async getList(): Promise<User[]> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;
    const token = this.authenticationService.connection?.token;
    if (!token) {
      return [];
    }

    return new Promise<User[]>((resolve) => {
      this.http.get<User[]>(`${backendUrl}/Users/list`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }).subscribe({
        next: (response) => {
          resolve(response);
        },
        error: () => {
          resolve([]);
        }
      });
    });
  }

  async create(user: User): Promise<void> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;
    const token = this.authenticationService.connection?.token;
    if (!token) {
      return;
    }

    return new Promise<void>((resolve) => {
      this.http.post(`${backendUrl}/Users/create`, user, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }).subscribe({
        next: () => {
          resolve();
        },
        error: () => {
          resolve();
        }
      });
    });
  }

  async update(user: User): Promise<void> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;
    const token = this.authenticationService.connection?.token;
    if (!token) {
      return;
    }

    return new Promise<void>((resolve) => {
      this.http.post(`${backendUrl}/Users/update`, user, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }).subscribe({
        next: () => {
          resolve();
        },
        error: () => {
          resolve();
        }
      });
    });
  }

  async delete(username: string): Promise<void> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;
    const token = this.authenticationService.connection?.token;
    if (!token) {
      return;
    }

    return new Promise<void>((resolve) => {
      this.http.delete(`${backendUrl}/Users/delete?username=${encodeURIComponent(username)}`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }).subscribe({
        next: () => {
          resolve();
        },
        error: () => {
          resolve();
        }
      });
    });
  }

}
