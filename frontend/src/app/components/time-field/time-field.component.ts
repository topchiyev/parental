import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-time-field',
  templateUrl: './time-field.component.html',
  styleUrls: ['./time-field.component.scss']
})
export class TimeFieldComponent {

  @Input() public time: number = 0;
  @Output() public timeChange = new EventEmitter<number>();

  get timeString(): string | undefined {
    if (this.time == undefined)
      return undefined;

    const hours = Math.floor(this.time / 3600);
    const minutes = Math.floor((this.time % 3600) / 60);
    const value = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
    return value;
  }

  set timeString(value: string | undefined) {
    if (!value) {
      this.time = 0;
      this.timeChange.emit(this.time);
      return;
    }

    const parts = value.split(':');
    if (parts.length != 2)
      return;

    const hours = parseInt(parts[0]);
    const minutes = parseInt(parts[1]);
    if (isNaN(hours) || isNaN(minutes))
      return;

    this.time = hours * 3600 + minutes * 60;
    this.timeChange.emit(this.time);
  }
}
