import { TimeRange } from "./TimeRange";

export interface Device {
  id: string;
  username: string;
  name: string;
  lastHandshakeOn: number;
  isManuallyLocked: boolean;
  lockedRanges: TimeRange[];
}
