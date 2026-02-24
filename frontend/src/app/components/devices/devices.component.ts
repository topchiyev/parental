import { Component, TemplateRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { Connection } from 'src/app/models/connection';
import { Device } from 'src/app/models/entity/Device';
import { AuthenticationService } from 'src/app/services/authentication.service';
import { EnvironmentService } from 'src/app/services/environment.service';
import { DevicesService } from 'src/app/services/devices.service';
import { UserRoleType } from 'src/app/models/enum/UserRoleType';
import { User } from 'src/app/models/entity/User';
import { UsersService } from 'src/app/services/users.service';

@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html',
  styleUrls: ['./devices.component.scss']
})
export class DevicesComponent {
  UserRoleType = UserRoleType;

  users: User[] = [];
  devices: Device[] = [];
  connection?: Connection;

  currentDevice?: Device;

  @ViewChild('deviceEditorTemplate') deviceEditorTemplate?: TemplateRef<any>;
	deviceEditorModalRef?: BsModalRef;


  constructor(
    private router: Router,
    private environmentService: EnvironmentService,
    private authenticationService: AuthenticationService,
    private usersService: UsersService,
    private devicesService: DevicesService,
    private modalService: BsModalService,
  ) {
    this.load();
  }

  private async load(): Promise<void> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    await new Promise(resolve => setTimeout(resolve, 300));
    if (!this.authenticationService.connection) {
      this.router.navigate(['/login']);
      return;
    }
    this.connection = this.authenticationService.connection;
    this.loadUsers();
    this.loadDevices();
  }

  private async loadUsers(): Promise<void> {
    if (this.connection!.roleType == UserRoleType.ADMIN) {
      this.users = (await this.usersService.getList()).filter(t => t.roleType == UserRoleType.USER);
    } else {
      this.users = [ { username: this.connection!.username, roleType: this.connection!.roleType, password: '' } ]
    }
  }

  private async loadDevices(): Promise<void> {
    this.devices = await this.devicesService.getList();
  }

  showDeviceEditor(device: Device) {
		if (device == null)
			return;

		this.currentDevice = { ...device };

		this.deviceEditorModalRef = this.modalService.show(this.deviceEditorTemplate!, { class: 'modal-md' });
	}

  closeDeviceEditor() {
    this.deviceEditorModalRef?.hide();
    this.currentDevice = undefined;
  }

	editDevice(id: string) {
		const device = this.devices.find(t => t.id == id);
		if (device == null)
			return;

		this.showDeviceEditor(device);
	}

	createDevice() {
		const device: Device = {
      id: '',
      username: this.users.find(t => true)?.username || '',
      name: '',
      lastHandshakeOn: 0,
      isManuallyLocked: false,
      lockedRanges: []
		};
		this.showDeviceEditor(device);
	}

  async submitDeviceEditor() {
    if (!this.currentDevice)
      return;

    await this.devicesService.update(this.currentDevice);

    this.loadDevices();
    this.closeDeviceEditor();
  }

  async deleteDevice(id: string) {
    if (!confirm(`Are you sure you want to delete device "${id}"?`)) {
      return;
    }

    await this.devicesService.delete(id);
    this.loadDevices();
  }

  addLockedRange() {
    if (!this.currentDevice)
      return;

    this.currentDevice.lockedRanges.push({ startTime: 0, endTime: 0 });
  }

}
