package com.user.management.dtos;

import jakarta.validation.constraints.NotBlank;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class UserRequestDTO {
    @NotBlank(message = "Required name")
    private String name;

    @NotBlank(message = "Required login")
    private String login;

    @NotBlank(message = "Required password")
    private String password;

}
