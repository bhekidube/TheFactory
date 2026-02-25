export interface SiteConfig {
  brandName: string;
  logoUrl: string;
  primaryColor: string;
  features: {
    [key: string]: boolean;
  };
}
