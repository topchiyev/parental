import { Injectable } from '@angular/core';

interface Environment {
  backendUrl: string;
}

@Injectable({ providedIn: 'root' })
export class EnvironmentService {
	environmentUrl = 'assets/environments/environment.json';
  environment: Environment = {
		backendUrl: ''
	};

  isReady(): boolean {
    return this.environment.backendUrl !== '';
  }

  constructor() {
    this.load();
  }

	async load(): Promise<void> {
		const resp = await fetch(this.environmentUrl);
		const json: Environment = JSON.parse(await resp.text());

		json.backendUrl = json.backendUrl.replace('*', window.location.hostname);

		this.environment = json;
  }

	public get backendUrl(): string {
		return this.environment.backendUrl;
	}

}
