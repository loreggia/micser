// ports apart from the dev engine (5080) and Vite (5173), so the tests can run next to them
export const enginePort = 5180;
export const webPort = 5181;

export const engineUrl = `http://127.0.0.1:${enginePort}`;
export const webUrl = `http://127.0.0.1:${webPort}`;
