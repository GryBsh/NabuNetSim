const path = require('path');

module.exports = { 
  mode: "development",
  devtool: 'source-map',
  entry: {
    protocol: './protocol.ts'
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
  },
  output: {
    filename: '[name].js',
    path: path.resolve(__dirname, 'bin'),
  },
};