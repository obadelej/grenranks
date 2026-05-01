const EVENT_DISPLAY_NAMES = {
  "100M": "100m",
  "200M": "200m",
  "400M": "400m",
  "800M": "800m",
  "1500M": "1500m",
  "3000M": "3000m",
  "5000M": "5000m",
  "10000M": "10000m",
  "100H": "100m Hurdles",
  "110H": "110m Hurdles",
  "400H": "400m Hurdles",
  LJ: "Long Jump",
  HJ: "High Jump",
  TJ: "Triple Jump",
  PV: "Pole Vault",
  SP: "Shot Put",
  DT: "Discus Throw",
  HT: "Hammer Throw",
  JT: "Javelin Throw",
};

export function toEventDisplayName(eventName) {
  if (!eventName) return "";
  const key = String(eventName).trim().toUpperCase();
  return EVENT_DISPLAY_NAMES[key] ?? String(eventName).trim();
}
