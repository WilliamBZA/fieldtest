Feature: Test Initialization
	In order to run tests on a remote process with the correct config files
	As a developer
	I want test execution to be broken into separate batches

Scenario: Valid configuration file should be copied to new file as expected
	Given I have a test assembly with a valid configuration file
	And no other test assemblies in my solution
	When I run the tests for that configuration
	Then the application configuration file should be copied to the remote test runner's file location