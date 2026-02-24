import { HttpClient } from '@angular/common/http';
import { EventEmitter, Injectable, Output } from "@angular/core";
import { EnvironmentService } from "./environment.service";
import { Connection } from '../models/connection';

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  connection?: Connection;
  @Output() onConnectionChange = new EventEmitter<Connection | undefined>();

  constructor(
    private environmentService: EnvironmentService,
    private http: HttpClient
  ) {
    this.tryRestoreConnection();
  }

  async login(username: string, password: string): Promise<boolean> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;

    this.logout();

    return new Promise<boolean>((resolve) => {
      this.http.post<Connection>(`${backendUrl}/Authentication/login`, { username, password }).subscribe({
        next: (response) => {
          if (response == null) {
            alert('Login failed: Invalid username or password');
            resolve(false);
          } else {
            this.connection = response;
            const json = JSON.stringify(this.connection);
            localStorage.setItem('connection', json);
            this.onConnectionChange.emit(this.connection);
            resolve(true);
          }
        },
        error: (error) => {
          alert('Login failed: ' + (error.error?.message || error.statusText || 'Unknown error'));
          resolve(false);
        }
      });
    });
  }

  logout(): void {
    this.connection = undefined;
    localStorage.removeItem('connection');
    this.onConnectionChange.emit(this.connection);
  }

  async validateConnection(connection: Connection): Promise<boolean> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;

    return new Promise<boolean>((resolve) => {
      this.http.get(`${backendUrl}/Authentication/validateConnection`, {
        headers: {
          Authorization: `Bearer ${connection.token}`
        }
      }).subscribe({
        next: () => resolve(true),
        error: () => resolve(false)
      });
    });
  }

  async tryRestoreConnection(): Promise<void> {
    const json = localStorage.getItem('connection');
    if (json != null) {
      let connection: Connection | undefined;
      try {
        connection = JSON.parse(json);
      } catch (error) {
        localStorage.removeItem('connection');
      }
      if (connection != null) {
        const isValid = await this.validateConnection(connection);
        if (isValid) {
          this.connection = connection;
          this.onConnectionChange.emit(this.connection);
        } else {
          localStorage.removeItem('connection');
        }
      }
    }
  }
}
