const config = {
  mongodb: {
    url: process.env.MONGODB_URI || "mongodb://localhost:27017",
    databaseName: process.env.MONGO_DB_NAME || "sd3",
    options: {
      connectTimeoutMS: 10000,
    },
  },
  migrationsDir: "scripts/migrations/mongo",
  changelogCollectionName: "changelog",
  migrationFileExtension: ".js",
  useFileHash: false,
  moduleSystem: "commonjs",
};

module.exports = config;
