package main

import (
	"encoding/json"
	"errors"
	"io/ioutil"
)

var credentialsMap map[string]map[string]interface{}

func readCredentials(credentialsFile string) error {
	credentialsData, err := ioutil.ReadFile(credentialsFile)
	if err != nil {
		return err
	}

	err = json.Unmarshal(credentialsData, &credentialsMap)
	if err != nil {
		return err
	}

	return nil
}

func getCredential(service string, key string) (interface{}, error) {
	if credentialsMap == nil {
		return "", errors.New("Credentials not read")
	}

	serviceMap, ok := credentialsMap[service]
	if !ok {
		return "", errors.New("Service not found in credentials file")
	}

	value, ok := serviceMap[key]
	if !ok {
		return "", errors.New("Key not found in service credentials")
	}

	return value, nil
}

func getCredentialString(service string, key string) (string, error) {
	value, err := getCredential(service, key)
	if err != nil {
		return "", err
	}

	stringValue, ok := value.(string)
	if !ok {
		return "", errors.New("Value is not a string")
	}

	return stringValue, nil
}

func getCredentialNumber(service string, key string) (float64, error) {
	value, err := getCredential(service, key)
	if err != nil {
		return 0, err
	}

	floatValue, ok := value.(float64)
	if !ok {
		return 0, errors.New("Value is not a float64")
	}

	return floatValue, nil
}
