package com.user.management.enums;

public enum Role {
    ADMIN("ADMIN"),
    OWNER("OWNER"),
    USER("USER");

    private String value;

    Role(String value) {
        this.value = value;
    }

    public String getValue() {
        return value;
    }
}
