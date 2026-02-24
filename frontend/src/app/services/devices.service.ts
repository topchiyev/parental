import { HttpClient } from '@angular/common/http';
import { Injectable } from "@angular/core";
import { EnvironmentService } from "./environment.service";
import { AuthenticationService } from './authentication.service';
import { Device } from '../models/entity/Device';

@Injectable({ providedIn: 'root' })
export class DevicesService {

  constructor(
    private environmentService: EnvironmentService,
    private authenticationService: AuthenticationService,
    private http: HttpClient
  ) { }

  async getList(username?: string): Promise<Device[]> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;
    const token = this.authenticationService.connection?.token;
    if (!token) {
      return [];
    }

    return new Promise<Device[]>((resolve) => {
      this.http.get<Device[]>(`${backendUrl}/Devices/list`, {
        headers: {
          Authorization: `Bearer ${token}`
        },
        params: {
          username: username || ''
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

  async get(id: string, handshake?: boolean): Promise<Device | null> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    const backendUrl = this.environmentService.backendUrl;
    const token = this.authenticationService.connection?.token;
    if (!token) {
      return null;
    }

    return new Promise<Device | null>((resolve) => {
      this.http.get<Device>(`${backendUrl}/Devices/get/${id}`, {
        headers: {
          Authorization: `Bearer ${token}`
        },
        params: {
          handshake: handshake == true ? 'true' : 'false'
        }
      }).subscribe({
        next: (response) => {
          resolve(response);
        },
        error: () => {
          resolve(null);
        }
      });
    });
  }

  async update(device: Device): Promise<void> {
      while (!this.environmentService.isReady()) {
        await new Promise(resolve => setTimeout(resolve, 100));
      }
      const backendUrl = this.environmentService.backendUrl;
      const token = this.authenticationService.connection?.token;
      if (!token) {
        return;
      }

      return new Promise<void>((resolve) => {
        this.http.post(`${backendUrl}/Devices/update`, device, {
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

    async delete(id: string): Promise<void> {
      while (!this.environmentService.isReady()) {
        await new Promise(resolve => setTimeout(resolve, 100));
      }
      const backendUrl = this.environmentService.backendUrl;
      const token = this.authenticationService.connection?.token;
      if (!token) {
        return;
      }

      return new Promise<void>((resolve) => {
        this.http.delete(`${backendUrl}/Devices/delete?id=${encodeURIComponent(id)}`, {
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
