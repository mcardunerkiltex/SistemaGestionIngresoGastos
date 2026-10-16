export interface User {
  id: string;
  email: string;
  displayName: string;
  hasPassword: boolean;
  hasGoogle: boolean;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface AuthConfig {
  googleClientId: string | null;
}
