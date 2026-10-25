export default {
  name: "SoulsFormats integration tests",
  historyPath: process.env.ALLURE_HISTORY_PATH,
  plugins: {
    awesome: {
      options: {
        reportName: "SoulsFormats integration tests",
        groupBy: ["parentSuite", "suite"],
      },
    },
  },
};
